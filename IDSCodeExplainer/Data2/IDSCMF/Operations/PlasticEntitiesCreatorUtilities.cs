using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Plane = Rhino.Geometry.Plane;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Operations
{
    public static class PlasticEntitiesCreatorUtilities
    {
        public static IEnumerable<Curve> GenerateImplantImprintOutlines(Mesh actualImplantWithoutSubtract, Mesh constraintMesh)
        {
            Wrap.PerformWrap(new Mesh[] { constraintMesh }, 1, 3, 0.5, false, false,
                true, false, out var shellMeshWrapped);
            return MeshIntersectionCurve.IntersectionCurve(actualImplantWithoutSubtract, shellMeshWrapped);
        }

        public static bool GenerateAdditionalSolidGuideComponents(GuideCreatorHelper.CreateGuideParameters guideCreationParams, out Mesh finalAdditionalComponents)
        {
            finalAdditionalComponents = null;

            var guideScrews = guideCreationParams.GuideScrews;
            var osteotomyMesh = guideCreationParams.OsteotomyMesh;
            var flanges = guideCreationParams.Flanges;
            var barrelsShape = guideCreationParams.BarrelsShape;
            var unprocessedBarrelsShape = guideCreationParams.UnprocessedBarrelsShape.Values.ToList();
            var bridges = guideCreationParams.Bridges;

            var additionalComponents = new Mesh();
            additionalComponents.Append(GuideCreatorComponentHelper.AddScrewEyesOrLabelTagSafe(guideScrews));
            additionalComponents.Append(flanges);
            additionalComponents.Append(GuideCreatorComponentHelper.AddBarrelsSafe(barrelsShape, osteotomyMesh));
            additionalComponents.Append(GuideCreatorComponentHelper.AddBarrelsSafe(unprocessedBarrelsShape, osteotomyMesh));
            additionalComponents.Append(AddGuideBridge(bridges));

            additionalComponents = MeshUtilities.RemoveNoiseShells(additionalComponents, 1);
            if (additionalComponents.Faces.Count == 0)
            {
                return false;
            }

            finalAdditionalComponents = AutoFix.PerformUnify(additionalComponents);
            return true;
        }

        private static Mesh AddGuideBridge(List<KeyValuePair<Brep, Plane>> guideBridges)
        {
            if (!guideBridges.Any())
            {
                return null;
            }

            var parameter = CMFPreferences.GetActualGuideParameters();
            var guideBridgesMesh = new Mesh();

            guideBridges.ForEach(x =>
            {
                x.Key.UserDictionary.TryGetString(AttributeKeys.KeyGuideBridgeType, out var bridgeType);
                var bridgeBrep = GuideBridgeUtilities.GetCompensatedGuideBridgeForLightweight(x.Key, x.Value, bridgeType == GuideBridgeType.OctagonalBridge
                    ? parameter.LightweightParams.OctagonalBridgeCompensation
                    : parameter.LightweightParams.SegmentRadius);
                var bridgeMesh = Mesh.CreateFromBrep(bridgeBrep, MeshParameters.IDS(GuideCreationParameters.MeshingParameterMinEdgeLength, GuideCreationParameters.MeshingParameterMaxEdgeLength));

                if (Wrap.PerformWrap(bridgeMesh, 0.05, 0, parameter.LightweightParams.SegmentRadius,
                        false, false, true, false, out var solidGuideBridge))
                {
                    guideBridgesMesh.Append(solidGuideBridge);
                }
            });

            return guideBridgesMesh;
        }

        public static bool GenerateSolidGuide(GuideCreatorHelper.CreateGuideParameters guideCreationParams,
            ref Dictionary<string, double> trackingParameters, out Mesh finalSolidGuide)
        {
            finalSolidGuide = null;
            
            var osteotomyMesh = guideCreationParams.OsteotomyMesh;
            var guideLinkSurfaces = guideCreationParams.LinkSurfaces;
            var guideSurfacesSmoothed = guideCreationParams.GuideSurfacesSmoothed;
            var surfaceWrap = guideCreationParams.GuideSurfaceWrap;
            var guideSupportRoI = guideCreationParams.GuideSupportRoI;

            if (!guideSurfacesSmoothed.Any())
            {
                return false;
            }

            var filteredGuideSurface = MeshUtilities.RemoveNoiseShells(MeshUtilities.AppendMeshes(guideSurfacesSmoothed), 1);

            // Wrap with offset = beam radius, so it will have similar size as light weight beam but in solid
            var parameters = CMFPreferences.GetActualGuideParameters();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            if (!Wrap.PerformWrap(new[] { filteredGuideSurface }, 0.125, 0, parameters.LightweightParams.SegmentRadius, 
                false, false, true, false, out var solidGuideBase))
            {
                return false;
            }
            stopwatch.Stop();
            trackingParameters.Add("PerformGuidePlasticEntitiesWrap", stopwatch.ElapsedMilliseconds * 0.001);

#if (INTERNAL)
            InternalUtilities.AddObject(solidGuideBase, "Solid Guide Base",
                $"Guide {guideCreationParams.NCase} Plastic Entities Intermediate");
#endif

            stopwatch.Restart();
            var cutSlotCreator = new GuideCutSlotCreator();
            if (!cutSlotCreator.CreateCutSlots(solidGuideBase, surfaceWrap, parameters.LightweightParams.SegmentRadius,
                guideLinkSurfaces, osteotomyMesh))
            {
                return false;
            }

            solidGuideBase = cutSlotCreator.ResGuideBaseWithCutSlot;
            stopwatch.Stop();
            trackingParameters.Add("CreateGuidePlasticEntitiesCutSlot", stopwatch.ElapsedMilliseconds * 0.001);

#if (INTERNAL)
            InternalUtilities.AddObject(solidGuideBase, "Solid Guide Base With Cut Slot",
                $"Guide {guideCreationParams.NCase} Plastic Entities Intermediate");
#endif

            // Get all additional guide component such as screw entities, flange and barrels for now
            // TBC: do guide bridge will need to include in future?
            stopwatch.Restart();
            if (GenerateAdditionalSolidGuideComponents(guideCreationParams, out var additionalComponents))
            {
#if (INTERNAL)
                InternalUtilities.AddObject(additionalComponents, "Additional Components",
                    $"Guide {guideCreationParams.NCase} Plastic Entities Intermediate");
#endif
                // Union other solid entites
                if (!Booleans.PerformBooleanUnion(out var solidGuide, new Mesh[] { additionalComponents, solidGuideBase }))
                {
                    return false;
                }
                solidGuide.Compact();
                finalSolidGuide = solidGuide;
            }
            else
            {
                finalSolidGuide = solidGuideBase;
            }
            stopwatch.Stop();
            trackingParameters.Add("GenerateGuidePlasticEntitiesAdditionalComponents", stopwatch.ElapsedMilliseconds * 0.001);


            stopwatch.Restart();
            finalSolidGuide = Booleans.PerformBooleanSubtraction(finalSolidGuide, guideSupportRoI);
            finalSolidGuide.Faces.CullDegenerateFaces();
            finalSolidGuide = finalSolidGuide.SplitDisjointPieces()
                .OrderByDescending(p => AreaMassProperties.Compute(p).Area).FirstOrDefault();
            stopwatch.Stop();
            trackingParameters.Add("FinalizeSolidGuidePlasticEntities", stopwatch.ElapsedMilliseconds * 0.001);

            if (finalSolidGuide == null)
            {
                return false;
            }

#if (INTERNAL)
            InternalUtilities.AddObject(finalSolidGuide, "Final Solid Guide",
                $"Guide {guideCreationParams.NCase} Plastic Entities Intermediate");
#endif
            return true;
        }

        public static bool GenerateGuideImprintOutlines(GuideCreatorHelper.CreateGuideParameters guideCreationParams, 
            ref Dictionary<string, double> trackingParameters, out List<Curve> imprintOutlines)
        {
            imprintOutlines = null;
            var guideSurfaceWrapRoIed = guideCreationParams.GuideSurfaceWrapRoI;

            if (!GenerateSolidGuide(guideCreationParams, ref trackingParameters, out var solidGuide))
            {
                return false;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            imprintOutlines = MeshIntersectionCurve.IntersectionCurve(solidGuide, guideSurfaceWrapRoIed);
            imprintOutlines = Curve.JoinCurves(imprintOutlines.Select(x => x.ToNurbsCurve()), DistanceParameters.Epsilon2Decimal).ToList();
            stopwatch.Stop();
            trackingParameters.Add("GenerateGuidePlasticEntitiesIsoCurve", stopwatch.ElapsedMilliseconds * 0.001);

            return true;
        }

        public static bool GenerateGuidePlasticEntities(CMFImplantDirector director, GuidePreferenceDataModel guidePrefDataModel, 
            ref Dictionary<string, double> trackingParameters, out Mesh guideImprintSubtractEntities,
            out Mesh guideScrewIndentationSubtractEntities)
        {
            guideImprintSubtractEntities = null;
            guideScrewIndentationSubtractEntities = null;

            var guideCreationParams = new GuideCreatorHelper.CreateGuideParameters(director, guidePrefDataModel);
            var guideComponents = new GuideCaseComponent();
            var objectManager = new CMFObjectManager(director);

            var buildingBlock =
                guideComponents.GetGuideBuildingBlock(IBB.ActualGuideImprintSubtractEntity, guidePrefDataModel);
            if (!objectManager.HasBuildingBlock(buildingBlock))
            {
                if (GenerateGuideImprintOutlines(guideCreationParams, ref trackingParameters, out var imprintOutlines))
                {
                    var generatedGuideImprintSubtractEntities = MeshUtilities.AppendMeshes(
                        GenerateGeneralImprintSubtractionEntities(imprintOutlines, guideCreationParams.GuideSupport, "Guide", ref trackingParameters));
                    objectManager.AddNewBuildingBlock(buildingBlock, generatedGuideImprintSubtractEntities);
                }
            }

            var guideImprintSubtractRhObject = objectManager.GetBuildingBlock(buildingBlock);
            guideImprintSubtractEntities = (Mesh) guideImprintSubtractRhObject.DuplicateGeometry();
            
            var guideScrews = guideCreationParams.GuideScrews;
            if (guideScrews != null && guideScrews.Any())
            {
                buildingBlock =
                    guideComponents.GetGuideBuildingBlock(IBB.GuideScrewIndentationSubtractEntity, guidePrefDataModel);
                if (!objectManager.HasBuildingBlock(buildingBlock))
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    var generatedGuideImprintSubtractEntities = MeshUtilities.AppendMeshes(
                        GenerateGeneralScrewIndentationSubtractionEntities(guideScrews,
                            guideCreationParams.GuideSupportRoI));
                    stopwatch.Stop();
                    trackingParameters.Add("GenerateGuidePlasticScrewsIndentation", stopwatch.ElapsedMilliseconds * 0.001);

                    objectManager.AddNewBuildingBlock(buildingBlock, generatedGuideImprintSubtractEntities);
                }

                var guidScrewIndentationSubtractRhObject = objectManager.GetBuildingBlock(buildingBlock);
                guideScrewIndentationSubtractEntities = (Mesh) guidScrewIndentationSubtractRhObject.DuplicateGeometry();
            }

            return true;
        }

        public static IEnumerable<Mesh> GenerateGeneralImprintSubtractionEntities(IEnumerable<Curve> curves, Mesh constraintMesh,
            string trackingImprintType, ref Dictionary<string, double> trackingParameters)
        {
            var imprintMeshes = new List<Mesh>();
            var sweepElapsedSec = 0.0;
            var fixingElapsedSec = 0.0;

            foreach (var curve in curves)
            {
                try
                {
                    if (!curve.IsClosed)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "An opened end curve found when generate imprint");
                        continue;
                    }

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    var curveOnMesh = curve.PullToMesh(constraintMesh, DistanceParameters.Epsilon2Decimal);

                    // This is a work around as some short curves will cause sweep operation to throw exception
                    // Sweep operation can be completed. However, the resulted mesh can be rough\not smooth.
                    if (curveOnMesh.GetLength() < 0.01)
                    {
                        continue;
                    }

                    // Generate 2 sweep and intersecting each other instead of generate a closed curve sweep
                    // This is a work around since some closed curve will cause sweep operation failed
                    var t10 = curveOnMesh.DivideByCount(10, true);

                    var c1 = curveOnMesh.Trim(t10[0], t10[3]);
                    var c2 = curveOnMesh.Trim(t10[2], t10[1]);

                    if (!Sweep.PerformCircularSweep(c1, 0.5, out var imprint1) ||
                        !Sweep.PerformCircularSweep(c2, 0.5, out var imprint2) ||
                        !Booleans.PerformBooleanUnion(out var rawImprint, new Mesh[] {imprint1, imprint2}))
                    {
                        continue;
                    }

                    stopwatch.Stop();
                    sweepElapsedSec += stopwatch.ElapsedMilliseconds * 0.001;

                    stopwatch.Restart();
                    var fixedImprint = MeshFixingUtilities.PerformMinimumFix(rawImprint, 1,
                        ImprintFixingParameters.ComplexSharpTriangleWidthThreshold,
                        ImprintFixingParameters.ComplexSharpTriangleAngelThreshold);
                    fixedImprint.Faces.CullDegenerateFaces();
                    stopwatch.Stop();
                    fixingElapsedSec += stopwatch.ElapsedMilliseconds * 0.001;

                    imprintMeshes.Add(fixedImprint);
                }
                catch (Exception ex)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"IDS failed to create a part of imprint subtraction entities due to: {ex.Message}");
                }
            }

            trackingParameters.Add($"Generated{trackingImprintType}ImprintSweep", sweepElapsedSec);
            trackingParameters.Add($"Perform{trackingImprintType}ImprintFixing", fixingElapsedSec);

            return imprintMeshes;
        }

        public static IEnumerable<Mesh> GenerateGeneralScrewIndentationSubtractionEntities(IEnumerable<Screw> screws, Mesh constraintMesh)
        {
            var screwIndentationSubtractionEntities = new List<Mesh>();
            foreach (var screw in screws)
            {
                var stamp = screw.GetScrewStamp();
                var intersectionPoly = MeshIntersectionCurve.IntersectionCurve(MeshUtilities.ConvertBrepToMesh(stamp), constraintMesh);
                var joinCurves = Curve.JoinCurves(intersectionPoly, DistanceParameters.Epsilon2Decimal).ToList();
                
                CurveUtilities.GetClosestPointFromCurves(joinCurves, screw.HeadPoint, out var screwIndentationOutline, out _);
                var screwIndentationSubtractionEntity= GenerateSolidExtrude(constraintMesh, screwIndentationOutline, screw.Direction, 1.1, -0.9);
                screwIndentationSubtractionEntities.Add(screwIndentationSubtractionEntity);
            }
            return screwIndentationSubtractionEntities;
        }

        public static Mesh GenerateSolidExtrude(Mesh constraintMesh, Curve curve, Vector3d direction, double offsetPositive, double offsetNegative)
        {
            if (offsetNegative > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offsetNegative), offsetNegative, "Value should be in negative");
            }

            if (offsetPositive < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offsetPositive), offsetNegative, "Value should be in positive");
            }

            var curveClone = curve.DuplicateCurve();
            if (!curveClone.MakeClosed(0))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to make curve closed when generate extrude");
                return null;
            }


            var splittedSurfaces = MeshOperations.SplitMeshWithCurves(constraintMesh, new List<Curve>() { curveClone }, true, true);
            var filteredSplittedSurfaces = MeshUtilities.FilterSmallMeshesByAreaMass(splittedSurfaces, 1.0);
            var extrudeEndSurface = filteredSplittedSurfaces[0];

            var topSurface = extrudeEndSurface.DuplicateMesh();
            if (offsetNegative < 0)
            {
                topSurface.Transform(Transform.Translation(direction * offsetNegative));
            }

            var bottomSurface = extrudeEndSurface.DuplicateMesh();
            if (offsetPositive > 0)
            {
                bottomSurface.Transform(Transform.Translation(direction * offsetPositive));
            }
            bottomSurface.Flip(true, true, true);

            var extrudeCurve = curveClone.DuplicateCurve();

            extrudeCurve.Transform(Transform.Translation(direction * offsetPositive));
            if (extrudeCurve.ClosedCurveOrientation(direction) == CurveOrientation.Undefined)
            {
                return null;
            }

            if (extrudeCurve.ClosedCurveOrientation(direction) == CurveOrientation.Clockwise)
            {
                extrudeCurve.Reverse();
            }

            var sideWall = MeshUtilities.ConvertBrepToMesh(
                Surface.CreateExtrusion(extrudeCurve, (direction * (offsetNegative - offsetPositive))).ToBrep(), true);

            var stitchedExtrude = MeshUtilities.AppendMeshes(new Mesh[] { topSurface, bottomSurface, sideWall });
            var fixedExtrude = AutoFix.PerformStitch(stitchedExtrude);
            fixedExtrude = AutoFix.PerformUnify(fixedExtrude);
            fixedExtrude.Faces.CullDegenerateFaces();
            return fixedExtrude;
        }
    }
}
