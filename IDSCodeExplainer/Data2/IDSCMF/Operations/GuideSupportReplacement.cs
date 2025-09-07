using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class GuideSupportReplacement
    {
        private readonly CMFImplantDirector _director;

        public Dictionary<string, List<string>> ErrorReporting { get; private set; }
        public Dictionary<string, List<string>> WarningReporting { get; private set; }

        private const string GuideSupportKey = "Guide Support";
        private const string GuideSurfaceWrapKey = "Guide Surface Wrap";
        private const string GuideFixationScrewKey = "Guide Fixation Screw";
        private const string GuideBridgeKey = "Guide Bridge";
        private const string GuideSurfaceKey = "Guide Surface";
        private const string GuideFlangeGuidingOutlineKey = "Flange Guiding Outline";

        public GuideSupportReplacement(CMFImplantDirector director)
        {
            this._director = director;
            ErrorReporting = new Dictionary<string, List<string>>();
            WarningReporting = new Dictionary<string, List<string>>();
        }
        
        public bool ReplaceGuideSupport(Mesh guideSupport, bool invalidateRoI)
        {
            if (guideSupport.SolidOrientation() != 1)
            {
                AddErrorReport(GuideSupportKey, "Mesh is not solid! Kindly check if the Stl(s) has holes and close it.");
                return false;
            }

            var guideSurfaceWrap = GenerateGuideSurfaceWrap(guideSupport);
            if (guideSurfaceWrap == null)
            {
                return false;
            }
            guideSurfaceWrap.FaceNormals.ComputeFaceNormals();

            var lowLodSurfaceWrap = GenerateLoDLowGuideSurfaceWrap(guideSurfaceWrap);
            if (!lowLodSurfaceWrap.IsValid)
            {
                WarningReporting[GuideSurfaceWrapKey] = new List<string>
                {
                    $"Low Lod Surface Wrap created is not valid."
                };
            }

            var attractedGuideScrew = AttractScrewFixation(lowLodSurfaceWrap);
            if (!attractedGuideScrew)
            {
                return false;
            }

            var attractedGuideBridge = AttractGuideBridge(lowLodSurfaceWrap);
            if (!attractedGuideBridge)
            {
                return false;
            }

            var attractedGuideSurfaces = RegenerateGuideSurfaces(lowLodSurfaceWrap);
            if (!attractedGuideSurfaces)
            {
                return false;
            }

            var objectManager = new CMFObjectManager(_director);
            var guideComponentIds = objectManager.GetAllBuildingBlockIds(IBB.GuidePreviewSmoothen).ToList();
            guideComponentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.ActualGuide));
            guideComponentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.GuideBaseWithLightweight));
            guideComponentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.SmoothGuideBaseSurface));
            guideComponentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.ActualGuideImprintSubtractEntity));
            guideComponentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.GuideScrewIndentationSubtractEntity));
            foreach (var id in guideComponentIds)
            {
                objectManager.DeleteObject(id);
            }

            var existingGuideSupport = objectManager.GetBuildingBlockId(IBB.GuideSupport);
            objectManager.SetBuildingBlock(IBB.GuideSupport, guideSupport, existingGuideSupport);

            var existingSurfaceWrap = objectManager.GetBuildingBlockId(IBB.GuideSurfaceWrap);
            var surfaceWrapId = objectManager.SetBuildingBlock(IBB.GuideSurfaceWrap, guideSurfaceWrap, existingSurfaceWrap);
            objectManager.SetBuildingBlockLoDLow(surfaceWrapId, lowLodSurfaceWrap);

            var regenerateGuideGuidingOutline = ProPlanImportUtilities.RegenerateGuideGuidingOutlines(objectManager);
            if (!regenerateGuideGuidingOutline)
            {
                WarningReporting[GuideFlangeGuidingOutlineKey] = new List<string>
                {
                    $"No Guide Flange Guiding outline created."
                };
            }

            var registrator = new CMFBarrelRegistrator(_director);
            bool areAllBarrelsMeetingSpecs;
            if (!registrator.RegisterAllGuideRegisteredBarrel(guideSupport, out areAllBarrelsMeetingSpecs))
            {
                registrator.Dispose();
                AddErrorReport("Registered Barrels",
                    $"Reg. Barrel(s) failed to be re-registered/leveled.");
                return false;
            }

            if (areAllBarrelsMeetingSpecs)
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Barrels meet specification.");
            }

            if (invalidateRoI && objectManager.HasBuildingBlock(IBB.GuideSupportRoI))
            {
                var roiId = objectManager.GetBuildingBlockId(IBB.GuideSupportRoI);
                if (objectManager.DeleteObject(roiId))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Guide Support RoI has been removed.");
                }

                var existingRemovedMetalIntegration = objectManager.GetBuildingBlock(IBB.GuideSupportRemovedMetalIntegrationRoI);
                if (existingRemovedMetalIntegration != null)
                {
                    objectManager.DeleteObject(existingRemovedMetalIntegration.Id);
                }

                _director.GuideManager.ResetGuideSupportRoICreationInformation();
            }

            _director.CasePrefManager.NotifyBuildingBlockHasChangedToAll(new[] { IBB.GuideFlangeGuidingOutline });
            return true;
        }   
        
        public Mesh GenerateGuideSurfaceWrap(Mesh support)
        {
            Mesh wrapped;
            if (!Wrap.PerformWrap(new[] {support}, 0.5, 0.0, 0.35, false, false, true, false, out wrapped))
            {
                AddErrorReport(GuideSurfaceWrapKey, "Wrapping has failed. Please ensure the support mesh is in good quality.");
                return null;
            }

            var parameter = CMFPreferences.GetActualGuideParameters();
            var firstRemeshParams = parameter.FirstRemeshParams;

            var guideSurfaceFirstRemeshed = wrapped.DuplicateMesh();
            for (var i = 0; i < firstRemeshParams.OperationCount; ++i)
            {
                guideSurfaceFirstRemeshed = ExternalToolInterop.PerformQualityPreservingReduceTriangles(guideSurfaceFirstRemeshed,
                firstRemeshParams.QualityThreshold, firstRemeshParams.MaximalGeometricError,
                firstRemeshParams.CheckMaximalEdgeLength, firstRemeshParams.MaximalEdgeLength,
                firstRemeshParams.NumberOfIterations, firstRemeshParams.SkipBadEdges,
                firstRemeshParams.PreserveSurfaceBorders);

                if (guideSurfaceFirstRemeshed == null)
                {
                    AddErrorReport(GuideSurfaceWrapKey, "Remeshing has failed. Kindly check your support mesh is in good quality. Contact development team if this remains a problem!");
                }
            }

            var filteredResult = AutoFix.RemoveNoiseShells(guideSurfaceFirstRemeshed);
            guideSurfaceFirstRemeshed?.Dispose();

            return filteredResult;
        }

        public Mesh GenerateLoDLowGuideSurfaceWrap(Mesh guideSurfaceWrap)
        {
            var objectManager = new CMFObjectManager(_director);

            var lowLodSurfaceWrap = objectManager.GenerateLoDLow(guideSurfaceWrap);

            return lowLodSurfaceWrap;
        }

        private bool AttractGuideBridge(Mesh guideSurfaceWrapLowLoD)
        {
            var objectManager = new CMFObjectManager(_director);
            var guideBridges = objectManager.GetAllBuildingBlocks(IBB.GuideBridge);
            
            foreach(var bridgeObj in guideBridges)
            {
                var gPref = objectManager.GetGuidePreference(bridgeObj);

                var bridge = (Brep)bridgeObj.Geometry;

                var getOcsSuccess = objectManager.GetBuildingBlockCoordinateSystem(bridgeObj.Id, out var ocs);
                if (!getOcsSuccess)
                {
                    AddErrorReport(GuideBridgeKey, $"Failed to get Object Coordinate System for one of the Guide Bridge for {gPref.CaseName}!");
                    return false;
                }

                //Now see changes
                var existingSurfaceWrap = objectManager.GetBuildingBlockId(IBB.GuideSurfaceWrap);
                objectManager.GetBuildingBlockLoDLow(existingSurfaceWrap, out var ExistingSurfaceWrapLowLod);

                var originalBridgeCenter = BrepUtilities.GetGravityCenter(bridge);
                var originalBridgeCenterClosestNewWrapLowLoDPt = guideSurfaceWrapLowLoD.ClosestMeshPoint(originalBridgeCenter, 30);
                var originalBridgeCenterClosestExistingWrapLowLoDPt = ExistingSurfaceWrapLowLod.ClosestMeshPoint(originalBridgeCenter, 30);

                if (originalBridgeCenterClosestNewWrapLowLoDPt == null || originalBridgeCenterClosestExistingWrapLowLoDPt == null)
                {
                    AddErrorReport(GuideBridgeKey, $"One of the Guide Bridge for {gPref.CaseName} failed to be positioned onto imported support mesh.");
                    return false;
                }

                var translation = Transform.Translation(originalBridgeCenterClosestNewWrapLowLoDPt.Point -
                                                        originalBridgeCenterClosestExistingWrapLowLoDPt.Point);
                var transformedBridge = bridge.DuplicateBrep();
                transformedBridge.Transform(translation);

                objectManager.GetBuildingBlockCoordinateSystem(bridgeObj.Id, out var bridgeOcs);

                var transformedBridgeOcs = bridgeOcs;
                transformedBridgeOcs.Transform(translation);

                var guideCaseComponent = new GuideCaseComponent();
                var guidePreferenceData = objectManager.GetGuidePreference(bridgeObj);
                var buildingBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideBridge, guidePreferenceData);                
                objectManager.SetBuildingBlock(buildingBlock, transformedBridge, bridgeObj.Id);

                objectManager.SetBuildingBlockCoordinateSystem(bridgeObj.Id, transformedBridgeOcs);
            }

            return true;
        }

        private bool AttractScrewFixation(Mesh guideSurfaceWrapLowLoD)
        {
            Locking.UnlockGuideFixationScrews(_director.Document);
            var objectManager = new CMFObjectManager(_director);
            var guideScrews = objectManager.GetAllBuildingBlocks(IBB.GuideFixationScrew);
            foreach(var screw in guideScrews)
            {
                var guidePref = objectManager.GetGuidePreference(screw);

                var guideScrew = (Screw)screw;

                var calibrator = new GuideFixationScrewCalibrator();

                var newHeadPt = calibrator.GetNewScrewHeadPoint(guideSurfaceWrapLowLoD, guideScrew);

                if (!newHeadPt.IsValid)
                {
                    AddErrorReport(GuideFixationScrewKey,
                        $"One of Guide Fixation Screw for {guidePref.CaseName} failed to be positioned onto imported support mesh.");
                    return false;
                }

                var translation = newHeadPt - guideScrew.HeadPoint;
                var newTipPt = guideScrew.TipPoint + translation;

                var repositionScrew = new Screw(_director, newHeadPt, newTipPt, guideScrew.ScrewAideDictionary, guideScrew.Index, guideScrew.ScrewType);

                var calibratedScrew = calibrator.LevelScrew(repositionScrew, guideSurfaceWrapLowLoD, guideScrew);
                if (calibratedScrew == null)
                {
                    AddErrorReport(GuideFixationScrewKey,
                        $"One of Guide Fixation Screw for {guidePref.CaseName} failed to be leveled onto imported support mesh.");
                    return false;
                }

                var casePreferenceData = objectManager.GetGuidePreference(guideScrew);
                var screwManager = new ScrewManager(_director);
                screwManager.ReplaceExistingScrewInDocument(calibratedScrew, ref guideScrew, casePreferenceData, false);
                calibratedScrew.UpdateAidesInDocument();
            }

            return true;
        }

        private bool RegenerateGuideSurfaces(Mesh guideSurfaceWrapLowLoD)
        {
            var dictionary = new Dictionary<GuidePreferenceDataModel, GuideSurfaceInfo>();
            var objectManager = new CMFObjectManager(_director);
            var guideComponent = new GuideCaseComponent();
            var osteotomies = ProPlanImportUtilities.GetAllOriginalOsteotomyPartsRhinoObjects(_director.Document).Select(o => (Mesh)o.Geometry).ToList();

            foreach (var guidePrefModel in _director.CasePrefManager.GuidePreferences)
            {
                var positiveSurfaces = guidePrefModel.PositiveSurfaces;
                var negativeSurfaces = guidePrefModel.NegativeSurfaces;
                var linkSurfaces = guidePrefModel.LinkSurfaces;
                var solidSurfaces = guidePrefModel.SolidSurfaces;

                if (!positiveSurfaces.Any() && !negativeSurfaces.Any() && !linkSurfaces.Any())
                {
                    continue;
                }

                //attract surfaces
                var attractedPositiveData = new List<PatchData>();
                foreach (var surface in positiveSurfaces)
                {
                    var attracted = AttractGuideSurfaces(guideSurfaceWrapLowLoD, surface);
                    if (attracted == null)
                    {
                        AddErrorReport(GuideSurfaceKey, $"One of Positive Patch for {guidePrefModel.CaseName} failed to be positioned onto imported support mesh.");
                        return false;
                    }
                    attractedPositiveData.Add(attracted);
                }

                var attractedNegativeData = new List<PatchData>();
                foreach (var surface in negativeSurfaces)
                {
                    var attracted = AttractGuideSurfaces(guideSurfaceWrapLowLoD, surface);
                    if (attracted == null)
                    {
                        AddErrorReport(GuideSurfaceKey, $"One of Negative Patch for {guidePrefModel.CaseName} failed to be positioned onto imported support mesh.");
                        return false;
                    }
                    attractedNegativeData.Add(attracted);
                }

                var attractedLinkData = new List<PatchData>();
                foreach (var surface in linkSurfaces)
                {
                    var attracted = AttractGuideSurfaces(guideSurfaceWrapLowLoD, surface);
                    if (attracted == null)
                    {
                        AddErrorReport(GuideSurfaceKey, $"One of Guide Link for {guidePrefModel.CaseName} failed to be positioned onto imported support mesh.");
                        return false;
                    }
                    attractedLinkData.Add(attracted);
                }

                var attractedSolidData = new List<PatchData>();
                foreach (var surface in solidSurfaces)
                {
                    var attracted = AttractGuideSurfaces(guideSurfaceWrapLowLoD, surface);
                    if (attracted == null)
                    {
                        AddErrorReport(GuideSurfaceKey, $"One of Solid Surfaces for {guidePrefModel.CaseName} failed to be positioned onto imported support mesh.");
                        return false;
                    }
                    attractedSolidData.Add(attracted);
                }

                var attractedPositiveSurfaces = attractedPositiveData.Select(d => d.Patch).ToList();
                var attractedNegativeSurfaces = attractedNegativeData.Select(d => d.Patch).ToList();
                var attractedLinkSurfaces = attractedLinkData.Select(d => d.Patch).ToList();
                var attractedSolidSurfaces = attractedSolidData.Select(d => d.Patch).ToList();
                var guideSurfaces = GuideSurfaceUtilities.CreateGuideSurfaces(attractedPositiveSurfaces, attractedNegativeSurfaces, attractedLinkSurfaces, 
                                                                              attractedSolidSurfaces, osteotomies, guideSurfaceWrapLowLoD, guidePrefModel.CaseName);
                if (guideSurfaces == null || !guideSurfaces.Any())
                {
                    AddErrorReport(GuideSurfaceKey, $"Guide Surface for {guidePrefModel.CaseName} failed to be created.");
                    return false;
                }

                dictionary.Add(guidePrefModel, new GuideSurfaceInfo
                {
                    PositiveSurfaces = attractedPositiveData,
                    NegativeSurfaces = attractedNegativeData,
                    LinkSurfaces = attractedLinkData,
                    SolidSurfaces = attractedSolidData,
                    GuideSurfaces = guideSurfaces
                });
            }

            //make changes to document
            foreach (var keyPairValue in dictionary)
            {
                var guidePrefModel = keyPairValue.Key;
                var guideInfo = keyPairValue.Value;

                guidePrefModel.PositiveSurfaces.Clear();
                guidePrefModel.PositiveSurfaces.AddRange(guideInfo.PositiveSurfaces);

                guidePrefModel.NegativeSurfaces.Clear();
                guidePrefModel.NegativeSurfaces.AddRange(guideInfo.NegativeSurfaces);

                guidePrefModel.LinkSurfaces.Clear();
                guidePrefModel.LinkSurfaces.AddRange(guideInfo.LinkSurfaces);

                guidePrefModel.SolidSurfaces.Clear();
                guidePrefModel.SolidSurfaces.AddRange(guideInfo.SolidSurfaces);

                var positiveGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.PositiveGuideDrawings, guidePrefModel);
                var negativeGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.NegativeGuideDrawing, guidePrefModel);
                var linkSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideLinkSurface, guidePrefModel);
                var solidSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideSolidSurface, guidePrefModel);
                var guideSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideSurface, guidePrefModel);

                var existingPositiveData = objectManager.GetAllBuildingBlocks(positiveGuideDrawingEibb);
                var existingNegativeData = objectManager.GetAllBuildingBlocks(negativeGuideDrawingEibb);
                var existingLinkData = objectManager.GetAllBuildingBlocks(linkSurfaceEibb);
                var existingSolidData = objectManager.GetAllBuildingBlocks(solidSurfaceEibb);
                var existingGuideSurfaceData = objectManager.GetAllBuildingBlocks(guideSurfaceEibb);

                foreach (var surface in existingPositiveData)
                {
                    objectManager.DeleteObject(surface.Id);
                }

                foreach (var surface in existingNegativeData)
                {
                    objectManager.DeleteObject(surface.Id);
                }

                foreach (var surface in existingLinkData)
                {
                    objectManager.DeleteObject(surface.Id);
                }

                foreach (var surface in existingSolidData)
                {
                    objectManager.DeleteObject(surface.Id);
                }

                foreach (var surface in existingGuideSurfaceData)
                {
                    objectManager.DeleteObject(surface.Id);
                }

                foreach (var surface in guidePrefModel.PositiveSurfaces)
                {
                    objectManager.AddNewBuildingBlock(positiveGuideDrawingEibb, surface.Patch);
                }

                foreach (var surface in guidePrefModel.NegativeSurfaces)
                {
                    objectManager.AddNewBuildingBlock(negativeGuideDrawingEibb, surface.Patch);
                }

                foreach (var surface in guidePrefModel.LinkSurfaces)
                {
                    objectManager.AddNewBuildingBlock(linkSurfaceEibb, surface.Patch);
                }

                foreach (var surface in guidePrefModel.SolidSurfaces)
                {
                    objectManager.AddNewBuildingBlock(solidSurfaceEibb, surface.Patch);
                }

                foreach (var surface in guideInfo.GuideSurfaces)
                {
                    objectManager.AddNewBuildingBlock(guideSurfaceEibb, surface);
                }
            }

            return true;
        }

        private PatchData AttractGuideSurfaces(Mesh guideSurfaceWrap, PatchData surfaceToAttract)
        {
            PatchData attractedSurface = null;

            if (surfaceToAttract.GuideSurfaceData is SkeletonSurface skeletonSurface)
            {
                var curves = new List<Curve>();
                var controlPoints = new List<List<Point3d>>();

                foreach (var points in skeletonSurface.ControlPoints)
                {
                    var attractedPoints = new List<Point3d>();
                    foreach (var point in points)
                    {
                        var meshPoint = guideSurfaceWrap.ClosestMeshPoint(point, 30.0);
                        if (meshPoint == null)
                        {
                            return null;
                        }
                        attractedPoints.Add(meshPoint.Point);
                    }

                    controlPoints.Add(attractedPoints);
                    curves.Add(CurveUtilities.BuildCurve(attractedPoints, 1, false));
                }

                var surface = GuideSurfaceUtilities.CreateSkeletonSurface(guideSurfaceWrap, curves, skeletonSurface.Diameter / 2);
                attractedSurface = new PatchData(surface)
                {
                    GuideSurfaceData = new SkeletonSurface
                    {
                        ControlPoints = controlPoints,
                        Diameter = skeletonSurface.Diameter,
                        IsNegative = skeletonSurface.IsNegative
                    }
                };
            }
            else
            {
                var patchSurface = (PatchSurface)surfaceToAttract.GuideSurfaceData;
                var controlPoints = new List<Point3d>();

                foreach (var point in patchSurface.ControlPoints)
                {
                    var meshPoint = guideSurfaceWrap.ClosestMeshPoint(point, 30.0);
                    if (meshPoint == null)
                    {
                        return null;
                    }
                    controlPoints.Add(meshPoint.Point);
                }

                var curve = CurveUtilities.BuildCurve(controlPoints, 1, true);
                var pulledCurve = curve.PullToMesh(guideSurfaceWrap, 0.1);
                var tube = GuideSurfaceUtilities.CreateCurveTube(pulledCurve, patchSurface.Diameter / 2);
                var surface = GuideSurfaceUtilities.CreatePatch(tube, guideSurfaceWrap, true);
                attractedSurface = new PatchData(surface)
                {
                    GuideSurfaceData = new PatchSurface
                    {
                        ControlPoints = controlPoints,
                        Diameter = patchSurface.Diameter,
                        IsNegative = patchSurface.IsNegative
                    }
                };
            }

            return attractedSurface;
        }

        private void AddErrorReport(string key, params string[] details)
        {
            if (!ErrorReporting.ContainsKey(key))
            {
                ErrorReporting[key] = new List<string>();
            }

            details.ToList().ForEach(x =>
            {
                ErrorReporting[key].Add($"-{x}");
                Msai.TrackException(new Exception(x), "CMF");
            });
        }

        internal class GuideSurfaceInfo
        {
            public List<PatchData> PositiveSurfaces { get; set; }
            public List<PatchData> NegativeSurfaces { get; set; }
            public List<PatchData> LinkSurfaces { get; set; }
            public List<PatchData> SolidSurfaces { get; set; }
            public List<Mesh> GuideSurfaces { get; set; }
        }
    }
}
