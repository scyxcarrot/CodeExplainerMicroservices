using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Visualization;
using IDS.Core.Operations;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS
{
    public class GuideCreator
    {
        private const double GuideCupThickness = 3.5; // after clearance this will be 3.2 as specified
        private const double GuideRingGuideCupOverlap = 0.1;
        private const double GuideRingGuideCupOverlapDegrees = 0.25;
        private const double GuideRingImplantCupOverlap = 0.1;
        private const double GuideRingOverhangAngleDegrees = 13;
        private readonly Cup _cup;
        private readonly List<Screw> _screws;
        private const int FlangeLiftTabUndulations = 7;
        private const double FlangeLiftTabIntersection = 6.1;
        private const int CupLiftTabUndulations = 5;
        private const double CupLiftTabIntersection = 4.1;

        /// <summary>
        /// Initializes a new instance of the <see cref="GuideCreator" /> class.
        /// </summary>
        /// <param name="cup">The cup.</param>
        /// <param name="screws">The screws.</param>
        public GuideCreator(Cup cup, List<Screw> screws)
        {
            _cup = cup;
            _screws = screws;
        }
        
        private static Point3d CircleCenter => Point3d.Origin;
        private Transform CupAlignRotation => Transform.Rotation(Vector3d.YAxis, _cup.orientation, Point3d.Origin);
        private Transform CupAlignTranslation => Transform.Translation(_cup.centerOfRotation - Point3d.Origin);
        private static Line CupRevolveAxis => new Line(0, 0, 0, 0, 1, 0);
        private double GuideRingShapeLateralEndAngleDegrees => _cup.apertureAngle / 2 + 3;
        private double GuideRingShapeLateralStartAngleDegrees => GuideCupShapeLateralEndAngleDegrees - GuideRingOverhangAngleDegrees;
        private double InnerGuideCupRadius => _cup.InnerCupRadius - GuideCupThickness;
        private double InnerGuideRingRadius => InnerGuideCupRadius - GuideRingGuideCupOverlap;
        private static MeshingParameters MeshParamaters => MeshParameters.IDS();
        private double OuterGuideCupRadius => _cup.InnerCupRadius;
        private double OuterGuideRingRadius => _cup.InnerCupRadius + GuideRingImplantCupOverlap;

        private double GuideCupShapeLateralEndAngleDegrees
        {
            get
            {
                var circleDesigner = new CircleDesigner(CircleCenter);
                const double arcLengthBetweenCupAndGuideCupHorizontalBorder = 2.5;
                var cupInnerCurve = circleDesigner.CreateCurveOnCircle(_cup.InnerCupRadius, _cup.apertureAngle/2);
                var cupHighestPoint = cupInnerCurve.PointAtEnd;
                var guideCupLateralHighestPoint = (Point3d)(cupHighestPoint - new Point3d(GuideCupThickness, 0, 0));
                var verticalReference = new Vector3d(0, -1, 0);
                var lateralGuideCupShapeCurveReference = new Vector3d(guideCupLateralHighestPoint - CircleCenter);
                var guideCupShapeLateralLevelAngleDegrees =
                    RhinoMath.ToDegrees(Vector3d.VectorAngle(verticalReference, lateralGuideCupShapeCurveReference));

                var guideCupShapeLateralEndAngleDegrees = guideCupShapeLateralLevelAngleDegrees -
                                                          MathUtilities.CalculateArcAngle(InnerGuideCupRadius,
                                                              arcLengthBetweenCupAndGuideCupHorizontalBorder);

                return guideCupShapeLateralEndAngleDegrees;
            }
        }

        /// <summary>
        /// Exports all guide entities.
        /// </summary>
        /// <param name="plateWithoutHoles">The flanges.</param>
        /// <param name="drillBitRadius">The drill bit radius.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <param name="caseId">The case identifier.</param>
        /// <param name="version">The version.</param>
        /// <param name="draft">The draft.</param>
        public void ExportAllGuideEntities(Mesh plateWithoutHoles, double drillBitRadius, string folderPath, string caseId, int version, int draft)
        {
            // Guide Cup
            ExportGuideCupEntity(folderPath, caseId, version, draft);
            // Guide Ring
            ExportGuideEntity(GetGuideRingMesh(), "Guide_Ring", folderPath, caseId, version, draft, Colors.Plastic);
            // Lift Tabs
            ExportGuideEntity(GetCupLiftTabMesh(), "Guide_Cup_Lift_Tab", folderPath, caseId, version, draft, Colors.Plastic);
            ExportGuideEntity(GetFlangeLiftTabMesh(), "Guide_Flange_Lift_Tab", folderPath, caseId, version, draft, Colors.Plastic);
            // Cup Offset
            ExportGuideEntity(GetOffsetCupWithStuds(), "Guide_Cup_Offset", folderPath, caseId, version, draft, Colors.GeneralGrey);
            // Cup Semisphere
            ExportGuideEntity(GetCupSemiSphereMesh(), "Guide_Cup_Semisphere", folderPath, caseId, version, draft, Colors.GeneralGrey);
            // Flange Guide Cylinder Parts
            ExportGuideEntities(GetLowerFlangeCylinderMeshes(), "Guide_Flange_Cylinder_Lower", folderPath, caseId, version, draft, Colors.Plastic);
            ExportGuideEntities(GetUpperFlangeCylinderMeshes(), "Guide_Flange_Cylinder_Upper", folderPath, caseId, version, draft, Colors.Plastic);
            // Cup Guide Cylinder
            ExportGuideEntities(GetCupCylindersMeshes(), "Guide_Cup_Cylinder", folderPath, caseId, version, draft, Colors.Plastic);
            // Guide Screw Hole Boolean
            ExportGuideEntities(GetGuideHoleBooleanMeshes(drillBitRadius), "Guide_Hole_Boolean", folderPath, caseId, version, draft, Colors.GeneralGrey);
            ExportGuideEntity(GetGuideHoleBooleanSpheresMesh(), "Guide_Hole_Boolean_Spheres", folderPath, caseId, version, draft, Colors.GeneralGrey);
            // Snap Fit Union entity
            ExportGuideEntities(GetSnapFits(), "Guide_Snap_Fit_Boolean_Add", folderPath, caseId, version, draft, Colors.Plastic);
            // Snap Fit Subtractor
            ExportGuideEntities(GetSnapFitSubtractorMeshes(), "Guide_Snap_Fit_Boolean_Sub", folderPath, caseId, version, draft, Colors.GeneralGrey);
            // Screwhole Plugs
            ExportGuideScrewHolePlugsEntity(plateWithoutHoles, folderPath, caseId, version, draft);
            // Acetabular Plane XML
            CupExporter.ExportAcetabularPlane(_cup.cupRimPlane, folderPath, caseId, version, draft);
        }

        public void ExportGuideCupEntity(string folderPath, string caseId, int version, int draft)
        {
            ExportGuideEntity(GetGuideCup(false), "Guide_Cup", folderPath, caseId, version, draft, Colors.Plastic);
        }

        public void ExportGuideScrewHolePlugsEntity(Mesh plateWithoutHoles, string folderPath, string caseId, int version, int draft)
        {
            ExportGuideEntity(GetScrewHolePlugs(plateWithoutHoles), "Guide_Screw_Hole_Plugs", folderPath, caseId, version, draft, Colors.GeneralGrey);
        }

        public Mesh GetScrewHolePlugs(Mesh plateWithoutHoles)
        {
            var screwHoleBooleans = new List<Mesh>();
            foreach (var screw in _screws)
            {
                var screwAideManager = new ScrewAideManager(screw, screw.Director.ScrewDatabase);
                screwHoleBooleans.Add(screwAideManager.GetSubtractorMesh());
            }

            // Wrap
            Mesh wrappedScrewHoleBooleans;
            //var mdckShrinkWrapParameters = new MDCKShrinkWrapParameters(0.4,1.0,0.7,true,true,true,false);
            Wrap.PerformWrap(screwHoleBooleans.ToArray(), 0.4, 1.0, 0.7, true, true, true, false, out wrappedScrewHoleBooleans);

            // Intersect wrapped booleans and flanges
            var screwHolePlugs = Booleans.PerformBooleanIntersection(wrappedScrewHoleBooleans, plateWithoutHoles);

            return screwHolePlugs;
        }

        /// <summary>
        /// Gets the cup cylinders.
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, Brep> GetCupCylinders()
        {
            var cupCylinders = new Dictionary<int, Brep>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var screw in _screws)
            {
                // ReSharper disable once InvertIf
                if (screw.positioning == ScrewPosition.Cup)
                {
                    var screwAideManager = new ScrewAideManager(screw, screw.Director.ScrewDatabase);
                    cupCylinders.Add(screw.Index, screwAideManager.GetGuideCupCylinderBrep());
                }
            }

            return cupCylinders;
        }

        /// <summary>
        /// Gets the cup semi sphere.
        /// </summary>
        /// <returns></returns>
        private Brep GetCupSemiSphere()
        {
            var innerCupSurface = _cup.innerCupSurface;
            const double capTolerance = 0.01;
            var cappedInnerCup = innerCupSurface.CapPlanarHoles(capTolerance);
            cappedInnerCup.Flip();

            return cappedInnerCup;
        }

        private Mesh GetCupSemiSphereMesh() => GetCupSemiSphere().GetCollisionMesh(MeshParamaters);

        /// <summary>
        /// Gets the fat cup with fenestrations.
        /// </summary>
        /// <returns></returns>
        private Brep GetFatCupWithFenestrations()
        {
            var importer = new ImporterViaRunScript();
            var resources = new AmaceResources();

            var fatCupWithFenestrations = importer.ImportStepAsBrep(resources.GuideFatCupWithFenestrationsStepFile);
            var fatCupWithFenestrationsBrep = fatCupWithFenestrations[0];

            fatCupWithFenestrationsBrep = AlignToCup(fatCupWithFenestrationsBrep);

            return fatCupWithFenestrationsBrep;
        }

        /// <summary>
        /// Gets the guide cup.
        /// </summary>
        /// <returns></returns>
        public Mesh GetGuideCup(bool includeCylinderWrap)
        {
            var fatCupWithFenestrations = GetFatCupWithFenestrationsMesh();

            Mesh guideCupRough; // Rough shape that will be intersected withactual shape to creat the guide cup
            if (includeCylinderWrap)
            {
                // Wrap guide cup and guide cup cylinders
                var guideCupCylinders = GetCupCylindersMeshes();
                var wrapMeshes = new List<Mesh> { fatCupWithFenestrations };
                wrapMeshes.AddRange(guideCupCylinders.Values);
                Wrap.PerformWrap(wrapMeshes.ToArray(), 0.2, 1.5, 0.0, false, true, false, false, out guideCupRough);
            }
            else
            {
                guideCupRough = fatCupWithFenestrations;
            }

            // Intersect with guide cup shape to obtain guide cup with surfaces to support guide cup cylinders
            var guideCupShape = GetGuideCupShapeMesh();
            var cupWithFenestrations = Booleans.PerformBooleanIntersection(guideCupRough, guideCupShape);

            return cupWithFenestrations;
        }

        /// <summary>
        /// Gets the guide cup shape.
        /// </summary>
        /// <returns></returns>
        private Brep GetGuideCupShape()
        {
            var guideCupShape = BrepUtilities.CreateEntityFromCurves(CupRevolveAxis, GetGuideCupShapeCurves());

            guideCupShape.Flip();
            guideCupShape = AlignToCup(guideCupShape);

            return guideCupShape;
        }

        /// <summary>
        /// Gets the guide cup shape.
        /// No fenestrations or cylinders, just the general outline.
        /// </summary>
        /// <returns></returns>
        public List<Curve> GetGuideCupShapeCurves()
        {
            var circleDesigner = new CircleDesigner(CircleCenter);

            // Lateral Curve
            var lateralGuideCupShapeCurve = circleDesigner.CreateCurveOnCircle(InnerGuideCupRadius, GuideCupShapeLateralEndAngleDegrees);
            // Medial Curve
            var medialGuideCupShapeCurveEnd = lateralGuideCupShapeCurve.PointAtEnd + new Point3d(GuideCupThickness, 0, 0);
            var verticalReference = new Vector3d(0, -1, 0);
            var medialGuideCupShapeCurveReference = new Vector3d(medialGuideCupShapeCurveEnd - CircleCenter);
            var medialGuideCupAngle =
                RhinoMath.ToDegrees(Vector3d.VectorAngle(verticalReference, medialGuideCupShapeCurveReference));
            var medialGuideCupShapeCurve = circleDesigner.CreateCurveOnCircle(OuterGuideCupRadius, medialGuideCupAngle);
            // Straight connection line
            var connectionLine = new Line(lateralGuideCupShapeCurve.PointAtEnd, medialGuideCupShapeCurve.PointAtEnd).ToNurbsCurve();

            var guideCupShapeCurves = new List<Curve>
                {
                    lateralGuideCupShapeCurve,
                    connectionLine,
                    medialGuideCupShapeCurve
                };

            return guideCupShapeCurves;
        }

        /// <summary>
        /// Gets the guide hole booleans.
        /// </summary>
        /// <param name="region">The region.</param> 
        /// <param name="drillBitRadius">The drill bit radius.</param>
        /// <returns></returns>
        public Dictionary<int, Brep> GetGuideHoleBooleans(ScrewPosition region, double drillBitRadius)
        {
            var guideHoleBooleans = new Dictionary<int, Brep>();

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var screw in _screws)
            {
                // ReSharper disable once InvertIf
                if (region == ScrewPosition.Any || screw.positioning == region)
                {
                    var screwGuideCreator = new ScrewGuideCreator();
                    guideHoleBooleans.Add(screw.Index, screwGuideCreator.GetGuideHoleBoolean(screw, drillBitRadius));
                }
            }

            return guideHoleBooleans;
        }

        public Dictionary<int, Brep> GetGuideHoleBooleanSpheres(ScrewPosition region)
        {
            var guideHoleBooleans = new Dictionary<int, Brep>();

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var screw in _screws)
            {
                // ReSharper disable once InvertIf
                if (region == ScrewPosition.Any || screw.positioning == region)
                {
                    var screwGuideCreator = new ScrewGuideCreator();
                    guideHoleBooleans.Add(screw.Index, screwGuideCreator.GetGuideHoleBooleanSphere(screw));
                }
            }

            return guideHoleBooleans;
        }

        /// <summary>
        /// Gets the guide ring.
        /// </summary>
        /// <returns></returns>
        private Brep GetGuideRing()
        {
            var guideRingShape = BrepUtilities.CreateEntityFromCurves(CupRevolveAxis, GetGuideRingCurves());

            guideRingShape.Flip();
            guideRingShape = AlignToCup(guideRingShape);

            return guideRingShape;
        }

        /// <summary>
        /// Gets the guide ring.
        /// </summary>
        /// <returns></returns>
        public List<Curve> GetGuideRingCurves()
        {
            var lateralGuideRingShapeCurve = GetLateralGuideRingShapeCurve();
            // Medial Curve
            var medialGuideCupShapeCurveStart = lateralGuideRingShapeCurve.PointAtStart + new Point3d(GuideCupThickness + 2 * GuideRingGuideCupOverlap, 0, 0);
            var verticalReference = new Vector3d(0, -1, 0);
            var medialGuideCupShapeCurveReference = new Vector3d(medialGuideCupShapeCurveStart - CircleCenter);
            var medialGuideRingStartAngle =
                RhinoMath.ToDegrees(Vector3d.VectorAngle(verticalReference, medialGuideCupShapeCurveReference));
            var circleDesigner = new CircleDesigner(CircleCenter);
            var medialGuideCupShapeCurve = circleDesigner.CreateCurveOnCircle(OuterGuideRingRadius, _cup.apertureAngle / 2 - GuideRingGuideCupOverlapDegrees, medialGuideRingStartAngle);
            // Line to close ring bottom
            var horizontalLineBottom = new Line(lateralGuideRingShapeCurve.PointAtStart, medialGuideCupShapeCurve.PointAtStart).ToNurbsCurve();
            // Waves
            const double majorCircleRadius = 0.75;
            const double minorCircleRadius = 0.5;
            const int undulations = 5;
            const double majorUndulationWidthDegrees = 120;
            const double minorUndulationWidthDegrees = 100;
            var wave = CreateWave(lateralGuideRingShapeCurve.PointAtEnd, majorCircleRadius, minorCircleRadius, undulations, majorUndulationWidthDegrees, minorUndulationWidthDegrees);
            // Lines of overhang
            var horizontalOverhangLine = new Line(new Point3d(wave.PointAtEnd.X, medialGuideCupShapeCurve.PointAtEnd.Y, medialGuideCupShapeCurve.PointAtEnd.Z), medialGuideCupShapeCurve.PointAtEnd).ToNurbsCurve();
            var verticalOverhangLine = new Line(wave.PointAtEnd, horizontalOverhangLine.PointAtStart).ToNurbsCurve();
            // Bottom corner rounding
            const double guideRingBottomCornerAngleRadius = 0.75;
            var p1 = lateralGuideRingShapeCurve.PointAtLength(guideRingBottomCornerAngleRadius);
            var p2 = lateralGuideRingShapeCurve.PointAtStart;
            var p3 = horizontalLineBottom.PointAtLength(guideRingBottomCornerAngleRadius);
            var bottomLeftCorner = Curve.CreateControlPointCurve(new List<Point3d>() {p3, p2, p1}, 3);
            // Adjust points to make room for rounding
            lateralGuideRingShapeCurve.SetStartPoint(p1);
            horizontalLineBottom.SetStartPoint(p3);

            var guideRingCurves = new List<Curve>()
            {
                bottomLeftCorner,
                lateralGuideRingShapeCurve,
                wave,
                verticalOverhangLine,
                horizontalOverhangLine,
                medialGuideCupShapeCurve,
                horizontalLineBottom
            };

            return guideRingCurves;
        }

        private Mesh GetGuideRingMesh() => GetGuideRing().GetCollisionMesh(MeshParamaters);

        /// <summary>
        /// Gets the lift tab.
        /// </summary>
        /// <returns></returns>
        private Brep GetLiftTab(int undulations, double cupIntersectionUndulation)
        {
            var liftTab = BrepUtilities.CreateEntityFromCurves(CupRevolveAxis, GetLiftTabCurves(undulations, cupIntersectionUndulation));

            liftTab.Flip();
            liftTab = AlignToCup(liftTab);

            return liftTab;
        }

        private Brep GetCupLiftTab()
        {
            return GetLiftTab(CupLiftTabUndulations, CupLiftTabIntersection);
        }

        private Brep GetFlangeLiftTab()
        {
            return GetLiftTab(FlangeLiftTabUndulations, FlangeLiftTabIntersection);
        }

        /// <summary>
        /// Gets the lift tab curves.
        /// </summary>
        /// <param name="numberOfUndulations">The number of undulations.</param>
        /// <param name="cupIntersectionUndulation">The cup intersection undulation.</param>
        /// <returns></returns>
        public List<Curve> GetLiftTabCurves(int numberOfUndulations, double cupIntersectionUndulation)
        {
            const double majorWaveRadius = 0.75;
            const double minorWaveRadius = 0.5;
            const int majorUndulationWidthDegrees = 120;
            const int minorUndulationWidthDegrees = 100;

            // Determine optimal offset into cup to show two large undulations and 1.5 small ones
            var floor = CreateWave(Point3d.Origin, majorWaveRadius, minorWaveRadius, (int)Math.Floor(cupIntersectionUndulation), majorUndulationWidthDegrees, minorUndulationWidthDegrees);
            var ceiling = CreateWave(Point3d.Origin, majorWaveRadius, minorWaveRadius, (int)Math.Ceiling(cupIntersectionUndulation), majorUndulationWidthDegrees, minorUndulationWidthDegrees);
            var floorLength = floor.GetLength();
            var ceilingLength = ceiling.GetLength();
            var remainingRatioOfOverlappingUndulation = cupIntersectionUndulation - Math.Truncate(cupIntersectionUndulation);
            var intersectionLength = floorLength + remainingRatioOfOverlappingUndulation * (ceilingLength - floorLength);
            var intersectionPointAtOrigin = ceiling.PointAtLength(intersectionLength);
            var distanceIntoCup = intersectionPointAtOrigin.X - floor.PointAtStart.X;

            const double cornerRadius = 1.3;
            const double overlapMargin = 0.2;
            const double distanceUnderLateralCurveTop = cornerRadius + overlapMargin;

            var lateralGuideCupShapeCurve = GetLateralGuideRingShapeCurve();
            var bottomLeftCornerStartPoint = new Point3d(lateralGuideCupShapeCurve.PointAtEnd.X - distanceIntoCup + cornerRadius,
                                            lateralGuideCupShapeCurve.PointAtEnd.Y - distanceUnderLateralCurveTop,
                                            lateralGuideCupShapeCurve.PointAtEnd.Z);

            // Construct curves and lines
            var bottomLeftCorner = CreateGuideRingWaveCurve(bottomLeftCornerStartPoint, cornerRadius, 360, 270);
            var wave = CreateWave(bottomLeftCorner.PointAtEnd, majorWaveRadius, minorWaveRadius, numberOfUndulations, majorUndulationWidthDegrees, minorUndulationWidthDegrees);
            var bottomRightCorner = CreateGuideRingWaveCurve(wave.PointAtEnd, cornerRadius, 90, 0);
            var bottomLine = new Line(bottomRightCorner.PointAtEnd, bottomLeftCorner.PointAtStart).ToNurbsCurve();

            // Rotate wave downward around intersection point
            var intersectionPoint = wave.PointAtLength(intersectionLength);
            const int angle = 10;
            wave.Rotate(RhinoMath.ToRadians(angle), Vector3d.ZAxis, intersectionPoint);
            bottomRightCorner.Rotate(RhinoMath.ToRadians(angle), Vector3d.ZAxis, intersectionPoint);
            bottomLine.Rotate(RhinoMath.ToRadians(angle), Vector3d.ZAxis, intersectionPoint);
            bottomLeftCorner.Rotate(RhinoMath.ToRadians(angle), Vector3d.ZAxis, intersectionPoint);

            var liftTabCurves = new List<Curve>()
            {
                wave,
                bottomRightCorner,
                bottomLine,
                bottomLeftCorner
            };

            return liftTabCurves;
        }

        private Mesh GetCupLiftTabMesh() => GetCupLiftTab().GetCollisionMesh(MeshParamaters);
        private Mesh GetFlangeLiftTabMesh() => GetFlangeLiftTab().GetCollisionMesh(MeshParamaters);

        /// <summary>
        /// Gets the lower flange cylinders.
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, Brep> GetLowerFlangeCylinders()
        {
            var flangeCylindersLower = new Dictionary<int, Brep>();
            foreach (var screw in _screws)
            {
                if (screw.positioning != ScrewPosition.Flange)
                {
                    continue;
                }
                var screwAideManager = new ScrewAideManager(screw, screw.Director.ScrewDatabase);
                flangeCylindersLower.Add(screw.Index, screwAideManager.GetGuideFlangeCylinderLowerBrep());
            }

            return flangeCylindersLower;
        }

        /// <summary>
        /// Gets the offset cup with studs.
        /// </summary>
        /// <returns></returns>
        private Mesh GetOffsetCupWithStuds()
        {
            var meshParamaters = MeshParameters.IDS();
            var cupMesh = _cup.BrepGeometry.GetCollisionMesh(meshParamaters);

            // Wrap for clearance
            Mesh wrappedCupAndStuds;
            Wrap.PerformWrap(new[] { cupMesh }, 0.3, 0.0, 0.25, false, true, false, false, out wrappedCupAndStuds);

            return wrappedCupAndStuds;
        }

        /// <summary>
        /// Gets the lift tab.
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, Mesh> GetSnapFits()
        {
            var snapFits = new Dictionary<int, Mesh>();
            var resources = new AmaceResources();

            Mesh snapFit;
            StlUtilities.StlBinary2RhinoMesh(resources.GuideSnapFitStl, out snapFit);

            foreach (var screw in _screws)
            {
                // ReSharper disable once InvertIf
                if (screw.positioning == ScrewPosition.Flange)
                {
                    var screwAideManager = new ScrewAideManager(screw, screw.Director.ScrewDatabase);
                    var screwAlignTransform = screwAideManager.AlignmentTransform;

                    var snapFitAlignedToScrew = snapFit.DuplicateMesh();
                    snapFitAlignedToScrew.Transform(screwAlignTransform);

                    snapFits.Add(screw.Index, snapFitAlignedToScrew);
                }
            }

            return snapFits;
        }

        /// <summary>
        /// Gets the lift tab.
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, Brep> GetSnapFitSubtractors()
        {
            var snapFitSubtractors = new Dictionary<int, Brep>();
            var resources = new AmaceResources();

            var importer = new ImporterViaRunScript();
            var snapFitSubtractorImport = importer.ImportStepAsBrep(resources.GuideSnapFitSubtractorStepFile);
            var snapFitSubtractor = snapFitSubtractorImport[0]; // only one shell

            foreach (var screw in _screws)
            {
                // ReSharper disable once InvertIf
                if (screw.positioning == ScrewPosition.Flange)
                {
                    var screwAideManager = new ScrewAideManager(screw, screw.Director.ScrewDatabase);
                    var screwAlignTransform = screwAideManager.AlignmentTransform;

                    var snapFitSubtractorAlignedToScrew = snapFitSubtractor.DuplicateBrep();
                    snapFitSubtractorAlignedToScrew.Transform(screwAlignTransform);

                    snapFitSubtractors.Add(screw.Index, snapFitSubtractorAlignedToScrew);
                }
            }

            return snapFitSubtractors;
        }

        /// <summary>
        /// Gets the upper flange cylinders.
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, Brep> GetUpperFlangeCylinders()
        {
            var flangeCylindersUpper = new Dictionary<int, Brep>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var screw in _screws)
            {
                // ReSharper disable once InvertIf
                if (screw.positioning == ScrewPosition.Flange)
                {
                    var screwAideManager = new ScrewAideManager(screw, screw.Director.ScrewDatabase);
                    flangeCylindersUpper.Add(screw.Index, screwAideManager.GetGuideFlangeCylinderUpperBrep());
                }
            }

            return flangeCylindersUpper;
        }

        /// <summary>
        /// Converts all brep in the list to a single combined mesh.
        /// </summary>
        /// <param name="breps">The breps.</param>
        /// <returns></returns>
        private static Dictionary<int, Mesh> ConvertBrepsToMeshes(IDictionary<int, Brep> breps)
        {
            var meshes = new Dictionary<int, Mesh>();
            foreach (var brep in breps)
            {
                var mesh = brep.Value.GetCollisionMesh(MeshParamaters);
                meshes.Add(brep.Key, mesh);
            }
            return meshes;
        }

        private static Mesh ConvertBrepListToSingleMesh(IDictionary<int, Brep> breps)
        {
            var singleBrep = new Brep();
            foreach (var brep in breps.Values)
            {
                singleBrep.Append(brep);
            }
            var singleMesh = singleBrep.GetCollisionMesh(MeshParamaters);
            return singleMesh;
        }

        /// <summary>
        /// Creates a single guide ring wave curve.
        /// </summary>
        /// <param name="leftMostPoint">The left most point.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="startAngle">The start angle.</param>
        /// <param name="endAngle">The end angle.</param>
        /// <returns></returns>
        private static Curve CreateGuideRingWaveCurve(Point3d leftMostPoint, double radius, double endAngle, double startAngle)
        {
            var circleCenter = Point3d.Origin;
            var circleDesigner = new CircleDesigner(circleCenter);
            var wave = circleDesigner.CreateCurveOnCircle(radius, endAngle, startAngle);

            if (startAngle >= 0)
            {
                wave.Reverse();
            }

            var moveWave = Transform.Translation(leftMostPoint - wave.PointAtStart);
            wave.Transform(moveWave);

            return wave;
        }

        /// <summary>
        /// Creates the wave.
        /// </summary>
        /// <param name="leftMostPoint">The left most point.</param>
        /// <param name="majorCircleRadius">The major circle radius.</param>
        /// <param name="minorCircleRadius">The minor circle radius.</param>
        /// <param name="numberOfUndulations">The number of undulations.</param>
        /// <returns></returns>
        private static Curve CreateWave(Point3d leftMostPoint, double majorCircleRadius, double minorCircleRadius, int numberOfUndulations, double majorUndulationWidthDegrees, double minorUndulationWidthDegrees)
        {
            var undulations = new List<Curve>();

            for (var i = 0; i < numberOfUndulations; i++)
            {
                Curve undulation;

                if (i == 0)
                {
                    const double endAngleFirstUndulation = 270; // leftmost point
                    undulation = CreateGuideRingWaveCurve(leftMostPoint, majorCircleRadius, endAngleFirstUndulation, 180 - majorUndulationWidthDegrees / 2);
                }
                else if (numberOfUndulations % 2 != 0 && i == numberOfUndulations - 1)
                {
                    const double startAngleLastUndulation = 90;
                    undulation = CreateGuideRingWaveCurve(undulations.Last().PointAtEnd, majorCircleRadius, 180 + majorUndulationWidthDegrees / 2, startAngleLastUndulation);
                }
                else if (i % 2 == 0)
                {   
                    undulation = CreateGuideRingWaveCurve(undulations.Last().PointAtEnd, majorCircleRadius, 180 + majorUndulationWidthDegrees / 2, 180- majorUndulationWidthDegrees / 2);
                }
                else
                {
                    undulation = CreateGuideRingWaveCurve(undulations.Last().PointAtEnd, minorCircleRadius, minorUndulationWidthDegrees / 2, -minorUndulationWidthDegrees / 2);
                }

                undulations.Add(undulation);
            }

            var wave = Curve.JoinCurves(undulations)[0];
            return wave;
        }

        /// <summary>
        /// Exports the guide entity.
        /// </summary>
        /// <param name="guideEntity">The guide cup.</param>
        /// <param name="name">The name.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <param name="caseId">The case identifier.</param>
        /// <param name="version">The version.</param>
        /// <param name="draft">The draft.</param>
        /// <param name="color">The color.</param>
        private static void ExportGuideEntity(Mesh guideEntity, string name, string folderPath, string caseId, int version, int draft, Color color)
        {
            var colorIntegers = new int[] { color.R, color.G, color.B };
            StlUtilities.RhinoMesh2StlBinary(guideEntity, System.IO.Path.Combine(folderPath, $"{caseId}_{name}_v{version}_draft{draft}.stl"), colorIntegers);
        }

        /// <summary>
        /// Exports the guide entities.
        /// </summary>
        /// <param name="guideScrewEntities">The guide screw entities in a dictionary that has the screw indices as keys and meshes as values.</param>
        /// <param name="name">The name.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <param name="caseId">The case identifier.</param>
        /// <param name="version">The version.</param>
        /// <param name="draft">The draft.</param>
        /// <param name="color">The color.</param>
        private static void ExportGuideEntities(Dictionary<int,Mesh> guideScrewEntities, string name, string folderPath, string caseId, int version, int draft, Color color)
        {
            var colorIntegers = new int[] { color.R, color.G, color.B };

            foreach (var guideScrewEntity in guideScrewEntities)
            {
                StlUtilities.RhinoMesh2StlBinary(guideScrewEntity.Value,
                    System.IO.Path.Combine(folderPath, $"{caseId}_{name}_{guideScrewEntity.Key}_v{version}_draft{draft}.stl"), colorIntegers);
            }
        }

        /// <summary>
        /// Aligns to cup.
        /// </summary>
        /// <param name="guideCupShape">The guide cup shape.</param>
        /// <returns></returns>
        private Brep AlignToCup(Brep guideCupShape)
        {
            guideCupShape.Transform(CupAlignRotation);
            guideCupShape.Transform(CupAlignTranslation);

            return guideCupShape;
        }

        private Dictionary<int, Mesh> GetCupCylindersMeshes() => ConvertBrepsToMeshes(GetCupCylinders());

        private Mesh GetFatCupWithFenestrationsMesh() => GetFatCupWithFenestrations().GetCollisionMesh(MeshParamaters);

        private Mesh GetGuideCupShapeMesh() => GetGuideCupShape().GetCollisionMesh(MeshParamaters);

        private Dictionary<int, Mesh> GetGuideHoleBooleanMeshes(double drillBitRadius) => ConvertBrepsToMeshes(GetGuideHoleBooleans(ScrewPosition.Any, drillBitRadius));

        private Mesh GetGuideHoleBooleanSpheresMesh() => ConvertBrepListToSingleMesh(GetGuideHoleBooleanSpheres(ScrewPosition.Any));

        /// <summary>
        /// Gets the lateral guide cup shape curve.
        /// </summary>
        /// <returns></returns>
        private Curve GetLateralGuideRingShapeCurve()
        {
            var circleDesigner = new CircleDesigner(CircleCenter);
            return circleDesigner.CreateCurveOnCircle(InnerGuideRingRadius, GuideRingShapeLateralEndAngleDegrees, GuideRingShapeLateralStartAngleDegrees);
        }

        private Dictionary<int, Mesh> GetLowerFlangeCylinderMeshes() => ConvertBrepsToMeshes(GetLowerFlangeCylinders());

        private Dictionary<int, Mesh> GetSnapFitSubtractorMeshes() => ConvertBrepsToMeshes(GetSnapFitSubtractors());

        private Dictionary<int, Mesh> GetUpperFlangeCylinderMeshes() => ConvertBrepsToMeshes(GetUpperFlangeCylinders());
    }
}