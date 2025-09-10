using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.RhinoInterface.Converter;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MeshDiagnostics = IDS.Core.V2.MTLS.Operation.MeshDiagnostics;
using Plane = Rhino.Geometry.Plane;

namespace IDS.CMF.Operations
{
    //WIP still, refactoring expected.
    public class GuideCreatorV2
    {
        public GuideBaseCreator GuidBaseCreatorOperator { get; private set; }
        public GuideBaseFloatingEntityConnectionTriangulator GuideBaseFloatingEntityConnectionTriangulatorOperator { get; private set; }
        public GuideCutSlotCreator GuideCutSlotCreator { get; private set; }
        public Mesh ResGuideCreated { get; private set; }
        public bool DoStlFixing { get; set; } = true;
        public bool IsNeedToDoManualQprt { get; private set; } = false;
        public bool FilterDisjointPieces { get; set; } = true;

        private readonly Mesh _guideBase;
        private readonly Mesh _guideSupport;
        private readonly Mesh _guideSurfaceWrap;
        private readonly GuideParams _parameter;
        private readonly List<Screw> _guideFixationScrew;
        private readonly List<Brep> _guideFixationScrewEyeSubtractor;
        private readonly GuidePreferenceDataModel _guidePreference;
        private readonly List<Mesh> _guideLinks;
        private readonly List<Mesh> _guideSolids;
        private readonly Mesh _osteotomyMesh;
        private readonly List<Mesh> _teethBlocks;
        private readonly List<KeyValuePair<Brep,Plane>> _guideBridges;
        private readonly List<Mesh> _flanges;
        private readonly List<Brep> _barrels;
        private readonly List<Mesh> _barrelsShape;
        private readonly List<Mesh> _barrelSubtractors;
        private readonly Dictionary<Guid, Mesh> _unprocessedBarrelsShape;
        private readonly Dictionary<Guid, Mesh> _unprocessedBarrelSubtractors;

        public GuideCreatorV2(Mesh guideBase, Mesh guideSurfaceWrap, Mesh guideSupport, GuideParams parameter,
            List<Screw> guideGuideFixationScrew, GuidePreferenceDataModel guidePref, List<Mesh> guideLinks,
            List<Mesh> guideSolids, Mesh osteotomyMesh, List<KeyValuePair<Brep, Plane>> guideBridges, List<Mesh> flanges,
            List<Brep> barrels, List<Mesh> barrelsShape, List<Mesh> barrelSubtractors, Dictionary<Guid, Mesh> unprocessedBarrelsShape, Dictionary<Guid, Mesh> unprocessedBarrelSubtractors, List<Mesh> teethBlocks)
        {
            _guideBase = guideBase;
            _guideSurfaceWrap = guideSurfaceWrap;
            _guideSupport = guideSupport;
            _parameter = parameter;
            _guideFixationScrew = guideGuideFixationScrew;
            _guidePreference = guidePref;
            _guideLinks = guideLinks;
            _guideSolids = guideSolids;
            _osteotomyMesh = osteotomyMesh;
            _guideBridges = guideBridges;
            _flanges = flanges;
            _barrels = barrels;
            _barrelsShape = barrelsShape;
            _barrelSubtractors = barrelSubtractors;
            _unprocessedBarrelsShape = unprocessedBarrelsShape;
            _unprocessedBarrelSubtractors = unprocessedBarrelSubtractors;
            _teethBlocks = teethBlocks;

            _guideSupport.FaceNormals.ComputeFaceNormals();

            _guideFixationScrewEyeSubtractor = new List<Brep>();
            guideGuideFixationScrew.ForEach(x =>
            {
                _guideFixationScrewEyeSubtractor.Add(TransformModelToScrew(guidePref.GuideScrewAideData.ScrewEyeSubtractor, x));
            });
        }

        public struct InputMeshesInfo
        {
            public string GuideName { get; set; }
            public int GuideSupportTriangleCount { get; set; }
            public int GuideSurfaceWrapTriangleCount { get; set; }
            public int GuideSupportVertexCount { get; set; }
            public int GuideSurfaceWrapVertexCount { get; set; }
            public List<Screw> GuideScrews { get; set; }
        }

        public InputMeshesInfo GetInputForOperationMeshInfo()
        {
            return new InputMeshesInfo()
            {
                GuideName = _guidePreference.CaseName,
                GuideSupportTriangleCount = _guideSupport.Faces.Count,
                GuideSurfaceWrapTriangleCount = _guideSurfaceWrap.Faces.Count,
                GuideSupportVertexCount = _guideSupport.Vertices.Count,
                GuideSurfaceWrapVertexCount = _guideSurfaceWrap.Vertices.Count,
                GuideScrews = _guideFixationScrew
            };
        }

        public bool CreateGuide(out List<Guid> failedBarrelGuids)
        {
            return CreateGuideWithTransitions(0.0, 0.0, out failedBarrelGuids);
        }

        // If either of the parameters is 0.0, the method will skip the transition creation.
        public bool CreateGuideWithTransitions(double transitionRadius, double transitionGapClosingDistance, out List<Guid> failedBarrelGuids)
        {
            failedBarrelGuids = new List<Guid>();

            //Make the guide surface
            GuidBaseCreatorOperator = new GuideBaseCreator();
            if (!GuidBaseCreatorOperator.CreateGuideBaseLightweight(_guideBase, _parameter))
            {
                return false;
            }

            var tmpGuideBase = GuidBaseCreatorOperator.ResGuideBase;
            tmpGuideBase.Compact();

            // Create Solid Surfaces if available
            if (_guideSolids.Any())
            {
                var solidMesh = CreateGuideSolidSurface(_guideSolids, _guideBase, _guideSurfaceWrap, _parameter, _guidePreference);

                // Perform subtract operation here so that we can remove the top of lightweight structure during union operation
                // 0.2mm is used here as the derivative of the lightweight structure's height
                var offsetSolid = _parameter.LightweightParams.SegmentRadius + 0.2;
                tmpGuideBase = Booleans.PerformBooleanSubtraction(new List<Mesh> { tmpGuideBase }, MeshUtilities.AppendMeshes(_guideSolids).Offset(-offsetSolid, true));

                if (!Booleans.PerformBooleanUnion(out tmpGuideBase, new Mesh[] { tmpGuideBase, solidMesh }))
                {
                    throw new IDSException("Failed to combine guide base and solid surface");
                }
            }

            //Create cut slots 
            GuideCutSlotCreator = new GuideCutSlotCreator();
            if (!GuideCutSlotCreator.CreateCutSlots(tmpGuideBase, _guideSurfaceWrap, _parameter.LightweightParams.SegmentRadius, _guideLinks, _osteotomyMesh))
            {
                return false;
            }

            tmpGuideBase = GuideCutSlotCreator.ResGuideBaseWithCutSlot;

            Mesh additionalComponents = new Mesh();
            Mesh screwEyes = null;
            if (_guideFixationScrew.Any())
            {
                screwEyes = AddScrewEyes();
                additionalComponents.Append(screwEyes);
            }

            Mesh guideBridges = null;
            if (_guideBridges.Any())
            {
                guideBridges = AddGuideBridge();
                additionalComponents.Append(guideBridges);
            }

            Mesh flanges = null;
            if (_flanges.Any())
            {
                flanges = AddFlanges();
                additionalComponents.Append(flanges);
            }

            Mesh barrels = null;
            if (_barrels.Any())
            {
                if (_barrels.Count != (_barrelSubtractors.Count + _unprocessedBarrelSubtractors.Count))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, 
                        "Registered Barrel and Barrel Subtractor is not in sync! Please recreate or adjust the screws to regenerate the missing subtractors.");
                    return false;
                }

                barrels = AddBarrels();
                additionalComponents.Append(barrels);
            }

            var tolerance = 1e-4;
            bool createTransitions = transitionRadius > tolerance && transitionGapClosingDistance > tolerance;
            if (createTransitions && (_barrels.Any() || _guideFixationScrew.Any()))
            {
                var transitions = CreateTransitions(transitionRadius, transitionGapClosingDistance, tmpGuideBase);
                additionalComponents.Append(transitions);
            }

            additionalComponents = MeshUtilities.RemoveNoiseShells(additionalComponents, 1);
            if (additionalComponents.Faces.Count > 0)
            {
                additionalComponents = AutoFix.PerformUnify(additionalComponents);
            }

            if (!Booleans.PerformBooleanUnion(
                out tmpGuideBase, new[] { additionalComponents, tmpGuideBase }))
            {
                return false;
            }
            tmpGuideBase.Compact();

            if (_unprocessedBarrelSubtractors.Any())
            {
                if (_unprocessedBarrelSubtractors.Count != _unprocessedBarrelsShape.Count ||
                    !_unprocessedBarrelSubtractors.Keys.All(
                        barrelGuid => _unprocessedBarrelsShape.TryGetValue(barrelGuid, out _)))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error,
                        "Unprocessed Barrel Shape and Barrel Subtractor is not in sync! Please recreate or adjust the screws to regenerate the missing subtractors.");
                    return false;
                }

                foreach (var barrelObject in _unprocessedBarrelSubtractors.Keys)
                {
                    var barrelSubtractor = _unprocessedBarrelSubtractors[barrelObject].DuplicateMesh();
                    barrelSubtractor = AutoFix.PerformUnify(barrelSubtractor);
                    tmpGuideBase =
                        Booleans.PerformBooleanSubtraction(tmpGuideBase, barrelSubtractor);
                    tmpGuideBase.Compact();

                    var barrelShapes = GuideCreatorComponentHelper.AddBarrels(new List<Mesh> { _unprocessedBarrelsShape[barrelObject].DuplicateMesh() }, _osteotomyMesh);
                    var subtraction =
                        Booleans.PerformBooleanSubtraction(barrelShapes, barrelSubtractor);

                    try
                    {
                        var isFilletSuccess = Fillet.PerformFillet(new IDSRhinoConsole(), RhinoMeshConverter.ToIDSMesh(subtraction), RhinoMeshConverter.ToIDSMesh(tmpGuideBase), 0.2, 0.1, out var filleted);
                        if (!isFilletSuccess)
                        {
                            throw new IDSException("Fillet Failed: The meshToPerformFillet and filletedMesh is the same");
                        }

                        tmpGuideBase = RhinoMeshConverter.ToRhinoMesh(filleted);
                    }
                    catch
                    {
                        subtraction = MeshUtilities.RemoveNoiseShells(subtraction, 1);
                        if (subtraction.Faces.Count > 0)
                        {
                            subtraction = AutoFix.PerformUnify(subtraction);
                        }
                        Booleans.PerformBooleanUnion(out tmpGuideBase, new[] { subtraction, tmpGuideBase });
                        failedBarrelGuids.Add(barrelObject);
                    }
                }

                if (failedBarrelGuids.Count > 0)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Failed to perform fillet for {failedBarrelGuids.Count} barrel(s) of Guide {_guidePreference.NCase}!");
                }
            }
            tmpGuideBase.Compact();

            //Create clearance
            tmpGuideBase = Booleans.PerformBooleanSubtraction(tmpGuideBase, _guideSupport);
            tmpGuideBase.Compact();

            //Subtract with subtractors
            if (screwEyes != null)
            {
                var screwEyeSubtractorMeshes = new Mesh();
                _guideFixationScrewEyeSubtractor.ForEach(x =>
                {
                    screwEyeSubtractorMeshes.Append(Mesh.CreateFromBrep(x,
                        MeshParameters.IDS(Constants.GuideCreationParameters.MeshingParameterMinEdgeLength,
                        Constants.GuideCreationParameters.MeshingParameterMaxEdgeLength)));
                });
                tmpGuideBase = Booleans.PerformBooleanSubtraction(tmpGuideBase, screwEyeSubtractorMeshes);
                tmpGuideBase.Compact();
            }

            if (_barrelSubtractors.Any())
            {
                var barrelSubtractor = MeshUtilities.AppendMeshes(_barrelSubtractors);
                barrelSubtractor = AutoFix.PerformUnify(barrelSubtractor);
                tmpGuideBase = 
                    Booleans.PerformBooleanSubtraction(tmpGuideBase, barrelSubtractor);
                tmpGuideBase.Compact();
            }
            tmpGuideBase.Compact();

            if (_teethBlocks.Any())
            {
                var teethBlock = new Mesh();
                teethBlock.Append(_teethBlocks);

                if (teethBlock.CollidesWith(tmpGuideBase, 0))
                {
                    teethBlock = AutoFix.PerformUnify(teethBlock);
                    Booleans.PerformBooleanUnion(out tmpGuideBase, new[] { tmpGuideBase, teethBlock });
                }
            }

            // Pick the biggest shell
            var tmpFilteredGuideBase = FilterGuideMesh(tmpGuideBase);

            if (tmpFilteredGuideBase == null)
            {
                return false;
            }

            if (DoStlFixing)
            {
                bool isNeedToDoManualQprt;
                ResGuideCreated = DoGuideStlFixing(tmpFilteredGuideBase, FilterDisjointPieces, _guidePreference.CaseName, out isNeedToDoManualQprt);
                IsNeedToDoManualQprt = isNeedToDoManualQprt;

                if (ResGuideCreated == null)
                {
                    return false;
                }
            }
            else
            {
                if (!tmpFilteredGuideBase.IsValid)
                {
                    return false;
                }

                ResGuideCreated = tmpFilteredGuideBase;
            }

            return true;
        }
        
        private Mesh CreateTransitions(double transitionRadius, double transitionGapClosingDistance, Mesh tmpGuideBase)
        {
            // Add barrel transitions
            // \todo Functions named Add... are confusing because they do not add anything. They create a new object and return it.
            var barrelAndEyeShapes = new List<Mesh> {
                    AddBarrels(), // Barrel shapes
                    AddScrewEyes() // Eye shapes
                };

            // Wrap barrel and eye shapes to obtain ROI
            var roiGapClosingDistance = 2.5;
            var roiLevelOfDetail = 0.3;
            var roiOffset = transitionRadius + 0.2;
            var roiReduceTriangles = true; // \todo double check
            var roiPreserveSharpFeatures = false;
            var roiProtectThinWalls = false;
            var roiPreserveSurfaces = false;
            Mesh roi;
            Wrap.PerformWrap(barrelAndEyeShapes.ToArray(), roiLevelOfDetail, roiGapClosingDistance, roiOffset,
                roiProtectThinWalls, roiReduceTriangles, roiPreserveSharpFeatures, roiPreserveSurfaces, out roi);

            // Intersect ROI and lightweight structure
            var roiLightweight = Booleans.PerformBooleanIntersection(tmpGuideBase, roi);

            // Merge lightweight ROI and barrel/eye shapes
            var barrelEyeShapesAndLightweightROI = new List<Mesh>();
            barrelEyeShapesAndLightweightROI.AddRange(barrelAndEyeShapes);
            barrelEyeShapesAndLightweightROI.Add(roiLightweight);

            // Wrap 1: inward
            var inwardGapClosingDistance = transitionGapClosingDistance;
            var inwardLevelOfDetail = 0.1;
            var inwardOffset = -(transitionRadius + 0.05);
            var inwardReduceTriangles = true; // \todo double check
            var inwardPreserveSharpFeatures = false;
            var inwardProtectThinWalls = false;
            var inwardPreserveSurfaces = false;
            Mesh inwardWrap;
            Wrap.PerformWrap(barrelEyeShapesAndLightweightROI.ToArray(), inwardLevelOfDetail, inwardGapClosingDistance, inwardOffset,
                inwardProtectThinWalls, inwardReduceTriangles, inwardPreserveSharpFeatures, inwardPreserveSurfaces, out inwardWrap);

            // Wrap 2: outward
            var outwardGapClosingDistance = transitionGapClosingDistance;
            var outwardLevelOfDetail = 0.1;
            var outwardOffset = transitionRadius;
            var outwardReduceTriangles = true; // \todo double check
            var outwardPreserveSharpFeatures = false;
            var outwardProtectThinWalls = false;
            var outwardPreserveSurfaces = false;
            Mesh transitions;
            Wrap.PerformWrap(new Mesh[] { inwardWrap }, outwardLevelOfDetail, outwardGapClosingDistance, outwardOffset,
                outwardProtectThinWalls, outwardReduceTriangles, outwardPreserveSharpFeatures, outwardPreserveSurfaces, out transitions);

            return transitions;
        }

        public static Mesh DoGuideStlFixing(Mesh tmpFilteredGuideBase, bool filterDisjointPieces, string caseName, out bool isNeedManualQprt)
        {
            isNeedManualQprt = false; 
            var tmpUnifiedGuideBase = AutoFix.PerformUnify(tmpFilteredGuideBase);
            tmpUnifiedGuideBase.Compact();

            var tmpStitchGuideBase = Stitch.PerformStitching(tmpUnifiedGuideBase, 0.01, 5);
            tmpStitchGuideBase.Compact();

            var intermediateMesh = AutoFix.PerformUnify(tmpStitchGuideBase);
            intermediateMesh = Triangles.PerformFilterSharpTriangles(intermediateMesh, 0.0010, 30.000);
            intermediateMesh = AutoFix.PerformUnify(intermediateMesh);

            Mesh tmpReducedTriangleGuideBase = null;
            var n = 0;
            do
            {
                tmpReducedTriangleGuideBase = ExternalToolInterop.
                    PerformQualityPreservingReduceTriangles(intermediateMesh, 0.3, 0.001, false, 0.1, 3,
                        false, false);

                if (tmpReducedTriangleGuideBase == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"QCA PerformQualityPreservingReduceTriangles failed, Retrying #{n+1}/{3}");
                    Msai.PublishToAzure();
                    Thread.Sleep(1000);
                }

                ++n;
            } while (tmpReducedTriangleGuideBase == null && n < 3);

            if (tmpReducedTriangleGuideBase == null)
            {
                isNeedManualQprt = true;
                IDSPluginHelper.WriteLine(LogCategory.Error, $"QCA PerformQualityPreservingReduceTriangles failed. User have to work on further fixing!");
                Msai.PublishToAzure();
                return intermediateMesh;
            }

            tmpReducedTriangleGuideBase.Compact();
            var filteredResGuideCreated = FilterGuideMesh(tmpReducedTriangleGuideBase, filterDisjointPieces, caseName);

            if (filteredResGuideCreated == null || !filteredResGuideCreated.IsValid)
            {
                return null;
            }

            return filteredResGuideCreated;
            //TODO: Improve this so it don't be processed forever, ever, and ever, after.
            //                 var tempUnifyResGuideCreated = AutoFix.PerformUnify(tmpReducedTriangleGuideBase);
            // 
            //                 var tmpAutoFixedGuideBase = AutoFix.PerformAutoFix(tempUnifyResGuideCreated, 5);
            // 
            //                 ResGuideCreated = AutoFix.PerformUnify(tmpAutoFixedGuideBase);
        }

        private Brep TransformModelToScrew(Brep itemToTransform, Screw toThisScrew)
        {
            var screwComponent = new Brep();
            screwComponent.Append(itemToTransform);
            screwComponent.Transform(toThisScrew.AlignmentTransform);
            return screwComponent;
        }

        private Mesh AddScrewEyes()
        {
            return GuideCreatorComponentHelper.AddScrewEyesOrLabelTag(_guideFixationScrew); 
        }

        private Mesh AddGuideBridge()
        {
            var guideBridgesLightweight = new Mesh();

            _guideBridges.ForEach(guideBridgeKvp =>
            {
                guideBridgeKvp.Key.UserDictionary.TryGetString(
                    AttributeKeys.KeyGuideBridgeType, out var bridgeType);
                var lwBridge = GuideBridgeUtilities.GenerateGuideBridgeWithLightweightFromBrep(
                    guideBridgeKvp.Key, guideBridgeKvp.Value,
                    _parameter.LightweightParams.SegmentRadius,
                    _parameter.LightweightParams.FractionalTriangleEdgeLength,
                    bridgeType == GuideBridgeType.OctagonalBridge
                        ? _parameter.LightweightParams.OctagonalBridgeCompensation
                        : 0.0);

                // if the guide bridge intersects the teeth block,
                // we need to split the bridge into two parts and only take the part intersecting the guide surface
                if (IsGuideBridgeIntersectingTeethBlock(lwBridge))
                {
                    lwBridge = GetSplitGuideBridge(lwBridge);
                }

                guideBridgesLightweight.Append(lwBridge);
            });

            if (_osteotomyMesh == null || !_osteotomyMesh.Vertices.Any())
            {
                return guideBridgesLightweight;
            }

            var guideBridgeSubtractionEntity = Booleans.PerformBooleanIntersection(_guideSurfaceWrap, _osteotomyMesh);

            if (!guideBridgeSubtractionEntity.Vertices.Any())
            {
                return guideBridgesLightweight;
            }

            var trimmedBridge = Booleans.PerformBooleanSubtraction(guideBridgesLightweight, guideBridgeSubtractionEntity);

            return trimmedBridge;
        }

        private bool IsGuideBridgeIntersectingTeethBlock(Mesh lightweightBridge)
        {
            if (!_teethBlocks.Any())
            {
                return false;
            }

            var teethBlocksIds = _teethBlocks.Select(RhinoMeshConverter.ToIDSMesh);
            var teethBlockCombined = MeshUtilitiesV2.AppendMeshes(teethBlocksIds);

            var lightweightBridgeIds = RhinoMeshConverter.ToIDSMesh(lightweightBridge);
            var console = new IDSRhinoConsole();
            var intersection = BooleansV2.PerformBooleanIntersection(console,
                    lightweightBridgeIds, teethBlockCombined);

            return intersection.Vertices.Any();
        }

        // steps are below
        // 1) Wrap the teeth block with -0.05 offset so that when bridge is boolean union with teeth block later, there is less intersecting triangles
        // 2) Boolean subtract the guide bridge with teeth block and also the guide support to get two parts
        // 3) take all the parts that are intersecting the guide surface and output them
        private Mesh GetSplitGuideBridge(Mesh lightWeightBridge)
        {
            var console = new IDSRhinoConsole();
            var teethBlocksIds = _teethBlocks.Select(RhinoMeshConverter.ToIDSMesh);

            // wrap -0.05 so that the bridge output will be slightly inside the teeth block
            // This gives less overlapping triangles when boolean union guide bridge and teeth block
            WrapV2.PerformWrap(console, teethBlocksIds.ToArray(), 0.2, 0.0, -0.05, false, false, false, false, out var wrappedTeethBlock);

            var lightweightBridgeIds = RhinoMeshConverter.ToIDSMesh(lightWeightBridge);
            var guideBridgeSubtractIds = BooleansV2.PerformBooleanSubtraction(
                console, lightweightBridgeIds, wrappedTeethBlock);

            var guideSupportIds = RhinoMeshConverter.ToIDSMesh(_guideSupport);
            guideBridgeSubtractIds =
                BooleansV2.PerformBooleanSubtraction(console, guideBridgeSubtractIds, guideSupportIds);
            var guideBridgeMeshesIds =
                MeshDiagnostics.SplitByShells(console, guideBridgeSubtractIds, out _);

            IMesh guideBridgeResult = new IDSMesh();
            var guideBaseIds = RhinoMeshConverter.ToIDSMesh(_guideBase);
            foreach (var guideBridgeMeshIds in guideBridgeMeshesIds)
            {
                var intersection = Curves.IntersectionCurve(
                    console, guideBridgeMeshIds, guideBaseIds);
                if (intersection.Any())
                {
                    guideBridgeResult = MeshUtilitiesV2.AppendMeshes(
                        new[] { guideBridgeResult, guideBridgeMeshIds });
                }
            }

            return RhinoMeshConverter.ToRhinoMesh(guideBridgeResult);
        }

        private Mesh AddFlanges()
        {
            return GuideCreatorComponentHelper.AddFlanges(_flanges, _osteotomyMesh, _guideSurfaceWrap);
        }

        private Mesh AddBarrels()
        {
            var barrelList = _barrelsShape.ToList();
            barrelList.AddRange(_unprocessedBarrelsShape.Values.ToList());
            return GuideCreatorComponentHelper.AddBarrels(barrelList, _osteotomyMesh);
        }

        private Mesh HandleAddConnectionToFloatingEntitiesOnGuideBase(Mesh guideBase)
        {

            var potentialFloatingEntities = new List<Brep>();

            var screwEyes = _guideFixationScrew.Select(x => x.GetScrewEye()).ToList();
            potentialFloatingEntities.AddRange(screwEyes);
            potentialFloatingEntities.AddRange(_barrels);

            if (!potentialFloatingEntities.Any())
            {
                return guideBase;
            }

            //If there is screw eye that is not connected to any triangle edges
            GuideBaseFloatingEntityConnectionTriangulatorOperator = new GuideBaseFloatingEntityConnectionTriangulator(GuidBaseCreatorOperator.IntGuideBaseSurface);
            
            if (!GuideBaseFloatingEntityConnectionTriangulatorOperator.GenerateAdditionalConnectorsIfEntityIsFloating(potentialFloatingEntities,
                _guideSupport, _parameter))
            {
                return null;
            }

            var connectors = GuideBaseFloatingEntityConnectionTriangulatorOperator.ResConnectors;
            if (!connectors.Any())
            {
                return GuideCutSlotCreator.ResGuideBaseWithCutSlot;
            }

            var appendedConnectors = MeshUtilities.AppendMeshes(connectors);
            if (!Booleans.PerformBooleanUnion(out var guideBasePostProcessed,
                new[] { appendedConnectors, GuideCutSlotCreator.ResGuideBaseWithCutSlot }))
            {
                return null;
            }

            return guideBasePostProcessed;
        }

        private Mesh FilterGuideMesh(Mesh guideMesh)
        {
            return FilterGuideMesh(guideMesh, FilterDisjointPieces, _guidePreference.CaseName);
        }

        private static Mesh FilterGuideMesh(Mesh guideMesh, bool filterDisjointPieces, string caseName)
        {
            var disjoints = GetDisjointMeshByArea(guideMesh, caseName);

            if (!filterDisjointPieces)
            {
                return MeshUtilities.AppendMeshes(disjoints);
            }

            if (disjoints.Count() > 1)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"Some disjoint pieces were filtered out.");
            }

            return disjoints.First();
        }


        private static IOrderedEnumerable<Mesh> GetDisjointMeshByArea(Mesh mesh, 
            string caseName)
        {
            var guide = AutoFix.RemoveNoiseShells(mesh);
            var disjoints = guide.SplitDisjointPieces().
                OrderByDescending(p => AreaMassProperties.Compute(p).Area);

            if (!disjoints.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"Filtering shells for {caseName} creation resulted in empty mesh, please check/adjust the guide and run again.");
                return null;
            }

            return disjoints;
        }

        public static Mesh CreateGuideSolidSurface(List<Mesh> guideSolid, Mesh guideBase, Mesh guideSurfaceWrap, GuideParams guideParams, GuidePreferenceDataModel guidePreference)
        {
            var offsetSolidMesh = GuideSurfaceUtilities.CreateOffset(guideSolid, Constants.GuideCreationParameters.SolidSurfaceOffset);
            var offsetGuideMesh = GuideSurfaceUtilities.CreateOffset(new List<Mesh> { guideBase }, Constants.GuideCreationParameters.GuideBaseOffset);

            var filteredOffsetSolid = FilterGuideMesh(offsetSolidMesh, false, guidePreference.CaseName);
            var filteredOffsetGuide = FilterGuideMesh(offsetGuideMesh, false, guidePreference.CaseName);

            // Intersect to get the region of interest for solid surface
            var intersectedSolidMesh = Booleans.PerformBooleanIntersection(filteredOffsetSolid, filteredOffsetGuide);
            intersectedSolidMesh.Faces.CullDegenerateFaces();
            intersectedSolidMesh = AutoFix.RemoveNoiseShells(intersectedSolidMesh);
            intersectedSolidMesh = AutoFix.PerformUnify(intersectedSolidMesh);

            var patchSurface = new List<Mesh>();
            var disjoints = intersectedSolidMesh.SplitDisjointPieces();
            var filteredDisjoints = MeshUtilities.FilterSmallMeshes(disjoints.ToList(), 2);

            // Shrink the isocurve of the patch and wrap with offset value
            foreach (var disjoint in filteredDisjoints)
            {
                var solidPatch = GuideSurfaceUtilities.CreatePatchWithSmoothing(disjoint, guideSurfaceWrap);
                var compensatedMesh = SurfaceUtilities.CreateCompensatedMesh(solidPatch, guideParams.NonMeshParams.NonMeshIsoCurveDistance);

                if (compensatedMesh == null)
                {
                    break;
                }

                if (!Wrap.PerformWrap(new Mesh[] { compensatedMesh }, 0.3, 0.0, guideParams.NonMeshParams.NonMeshHeight, false, true, false, false, out var wrappedMesh))
                {
                    throw new IDSException("Failed to create wrap for solid surface during guide preview");
                }

                patchSurface.Add(wrappedMesh);
            }

            return MeshUtilities.AppendMeshes(patchSurface);
        }
    }
}
