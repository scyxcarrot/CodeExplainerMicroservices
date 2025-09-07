using System;
using System.Linq;
using Rhino.FileIO;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;

#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS
{
    public class ScrewAideManager
    {
        public static string SuffixHeadContour = "_CONTOUR_HEAD";
        public static string SuffixHeadCalibrationContour = "_CONTOUR_HEAD_CALIBRATION";

        //public static string SuffixContainerContour = "_CONTOUR_CONTAINER";
        public static string SuffixContainerMesh = "_MESH_CONTAINER";
        //public static string SuffixContainerSurface = "_SURFACE_CONTAINER";

        //public static string SuffixCushionBooleanContour = "_CONTOUR_CUSHION_BOOL";
        public static string SuffixCushionBooleanMesh = "_MESH_CUSHION_BOOL";
        //public static string SuffixCushionBooleanSurface = "_SURFACE_CUSHION_BOOL";

        //public static string SuffixOutlineContour = "_CONTOUR_OUTLINE";
        public static string SuffixOutlineMesh = "_MESH_OUTLINE";
        //public static string SuffixOutlineSurface = "_SURFACE_OUTLINE";

        //public static string SuffixHorizontalBorderBumpContour = "_CONTOUR_HOR_BORDER_BUMP";
        public static string SuffixHorizontalBorderBumpMesh = "_MESH_HOR_BORDER_BUMP";
        //public static string SuffixHorizontalBorderBumpSurface = "_SURFACE_HOR_BORDER_BUMP";

        //public static string SuffixLateralBumpContour = "_CONTOUR_AUG_LAT";
        public static string SuffixLateralBumpMesh = "_MESH_AUG_LAT";
        //public static string SuffixLateralBumpSurface = "_SURFACE_AUG_LAT";

        //public static string SuffixMedialBumpContour = "_CONTOUR_AUG_MED";
        public static string SuffixMedialBumpMesh = "_MESH_AUG_MED";
        //public static string SuffixMedialBumpSurface = "_SURFACE_AUG_MED";

        //public static string SuffixPlasticBooleanContour = "_CONTOUR_PLASTIC_BOOL";
        public static string SuffixPlasticBooleanMesh = "_MESH_PLASTIC_BOOL";
        //public static string SuffixPlasticBooleanSurface = "_SURFACE_PLASTIC_BOOL";

        //public static string SuffixScaffoldBooleanContour = "_CONTOUR_MBV_BOOL";
        public static string SuffixScaffoldBooleanMesh = "_MESH_MBV_BOOL";
        //public static string SuffixScaffoldBooleanSurface = "_SURFACE_MBV_BOOL";

        public static string SuffixStudSelectorMesh = "_MESH_STUD_SELECTOR";
        //public static string SuffixStudSelectorOutline = "_CONTOUR_STUD_SELECTOR";
        //public static string SuffixStudSelectorSurface = "_SURFACE_STUD_SELECTOR";

        public static string SuffixSubtractorContour = "_CONTOUR_SUBTRACT";
        public static string SuffixSubtractorMesh = "_MESH_SUBTRACT";
        //public static string SuffixSubtractorSurface = "_SURFACE_SUBTRACT";

        //public static string SuffixGuideFlangeCylinderLowerContour = "_CONTOUR_FLCYLLOWER";
        public static string SuffixGuideFlangeCylinderLowerSurface = "_SURFACE_FLCYLLOWER";
        public static string SuffixGuideFlangeCylinderUpperContour = "_CONTOUR_FLCYLUPPER";
        public static string SuffixGuideFlangeCylinderUpperSurface = "_SURFACE_FLCYLUPPER";
        public static string SuffixGuideCupCylinderUpperContour = "_CONTOUR_CUPCYL";
        public static string SuffixGuideCupCylinderSurface = "_SURFACE_CUPCYL";

        private readonly Screw _screw;
        private readonly File3dm _screwDatabase;

        public static Vector3d ScrewAxisInDatabase => new Vector3d(0,0,-1);
        private static Line ScrewRevolveAxis => new Line(0, 0, 0, 0, 0, 1); // Z

        // WARNING adjusting this value will change the reported drill bit diameter in all existing projects.
        // Implememnt backwards compatibility!!!
        private const double DrillBitProductionMargin = 0.1;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrewAideManager" /> class.
        /// </summary>
        /// <param name="screw">The screw.</param>
        /// <param name="screwDatabase">The screw database.</param>
        public ScrewAideManager(Screw screw, File3dm screwDatabase)
        {
            _screw = screw;
            _screwDatabase = screwDatabase;
        }

        public static double ConvertToDrillBitRadius(double drillBitDiameter)
        {
            return drillBitDiameter / 2.0 + DrillBitProductionMargin;
        }

        public static double ConvertToDrillBitDiameter(double drillBitRadius)
        {
            return 2.0 * (drillBitRadius - DrillBitProductionMargin);
        }

        private static Point3d GetTransformedHeadPoint(ScrewBrandType screwBrandType, Point3d originPoint, Vector3d head2Tip, bool isCalibrationPoint)
        {
            head2Tip.Unitize();

            var dbHeadPoint = ScrewDatabaseDataRepository.Get().GetHeadPoint(screwBrandType);
            var dbHeadCalibrationPoint = ScrewDatabaseDataRepository.Get().GetHeadCalibrationPoint(screwBrandType);

            if (isCalibrationPoint && dbHeadPoint < dbHeadCalibrationPoint)
            {
                head2Tip.Reverse();
            }

            var head2CalibrationDistance = (dbHeadCalibrationPoint - dbHeadPoint).Length;
            return originPoint + head2Tip * head2CalibrationDistance;
        }

        public static Point3d GetHeadCalibrationPointTransformed(ScrewBrandType screwBrandType, Vector3d head2Tip, Point3d headPoint)
        {
            return GetTransformedHeadPoint(screwBrandType, headPoint, head2Tip, true);
        }

        public static Point3d GetHeadPointTransformed(ScrewBrandType screwBrandType, Vector3d head2Tip, Point3d headCalibrationPoint)
        {
            return GetTransformedHeadPoint(screwBrandType, headCalibrationPoint, head2Tip, false);
        }

        public static Transform GetAlignmentTransform(Vector3d orientation, Point3d headPoint, ScrewBrandType screwBrandType)
        {
            var headOrigin = ScrewDatabaseDataRepository.Get().GetHeadPoint(screwBrandType);
            var rotation = Transform.Rotation(-Plane.WorldXY.ZAxis, orientation, headOrigin);
            var translation = Transform.Translation(headPoint - headOrigin);
            var fullTransform = Transform.Multiply(translation, rotation);
            return fullTransform;
        }


        public static double GetHeadAndHeadCalibrationOffset(File3dm screwDatabase, ScrewBrandType screwBrandType)
        {
            var headOrigin = ScrewDatabaseDataRepository.Get().GetHeadPoint(screwBrandType);
            var headCalibrationOrigin = ScrewDatabaseDataRepository.Get().GetHeadCalibrationPoint(screwBrandType);
            return Math.Abs(headOrigin.Z - headCalibrationOrigin.Z);
        }

        public Transform AlignmentTransform => GetAlignmentTransform(_screw.Direction, _screw.HeadPoint, _screw.screwBrandType);

        private static GeometryBase GetScrewAideGeometry(File3dm screwDatabase, ScrewBrandType screwBrandType, string suffix)
        {
            // Get object from 3dm file
            var tag = $"{screwBrandType}{suffix}";
            var target = screwDatabase.Objects.FirstOrDefault(rhobj => string.Equals(rhobj.Attributes.Name, tag, StringComparison.InvariantCultureIgnoreCase));

            return target?.Geometry;
        }

        private static Curve GetScrewAideCurveGeometry(File3dm screwDatabase, ScrewBrandType screwBrandType, string suffix)
        {
            // Check if the suffix actually refers to a mesh
            VerifySuffix(suffix, "Contour");

            // Convert to curve, no need to check for null, returned immediately
            var curve = GetScrewAideGeometry(screwDatabase, screwBrandType, suffix) as Curve;
            return curve;
        }

        private Curve GetScrewAideCurveGeometry(string suffix)
        {
            return GetScrewAideCurveGeometry(_screwDatabase, _screw.screwBrandType, suffix);
        }

        private static void VerifySuffix(string suffix, string geometryType)
        {
            // Check if the suffix actually refers to a curve
            var suffixParts = suffix.Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
            if (!string.Equals(suffixParts[0], geometryType, StringComparison.CurrentCultureIgnoreCase))
            {
                throw new IDSException($"Suffix does not correspond to a {geometryType}");
            }

        }

        public Mesh GetScrewAideMeshGeometryAligned(string suffix)
        {
            // Check if the key actually refers to a mesh
            VerifySuffix(suffix, "Mesh");

            // Get geometry aligned to the screw, no need to check for null, returned immediately
            var solid = GetScrewAideGeometryAligned(suffix) as Mesh;
            return solid;
        }

        public Brep GetScrewAideBrepGeometryAligned(string suffix)
        {
            // Check if the key actually refers to a mesh
            VerifySuffix(suffix, "Surface");

            // Get geometry aligned to the screw, no need to check for null, returned immediately
            var brep = GetScrewAideGeometryAligned(suffix) as Brep;
            return brep;
        }

        public Brep GetScrewAideBrepGeometryAligned(Brep brepAlignedAroundZAxis)
        {
            // Get geometry aligned to the screw, no need to check for null, returned immediately
            var brep = GetScrewAideGeometryAligned(brepAlignedAroundZAxis) as Brep;
            return brep;
        }

        private GeometryBase GetScrewAideGeometryAligned(GeometryBase geometryAlignedAroundZAxis)
        {
            var alignedGeometry = geometryAlignedAroundZAxis.Duplicate();
            alignedGeometry.Transform(AlignmentTransform);

            return alignedGeometry;
        }

        private GeometryBase GetScrewAideGeometryAligned(string suffix)
        {
            // Get geometry centered at origin
            var geometry = GetScrewAideGeometry(_screwDatabase, _screw.screwBrandType, suffix);
            return geometry == null ? null : GetScrewAideGeometryAligned(geometry);
        }

        public double GetHeadRadius()
        {
            // Get head contour
            var headContour = GetHeadCurve(_screwDatabase, _screw.screwBrandType);
            return headContour.GetBoundingBox(true).Max.X;
        }

        public Point3d GetHeadCenter()
        {
            var headPointToHeadCenterDistance = GetHeadPointToHeadCenterDistance();
            // headCenter corresponds to offset from headpoint along screw direction
            var headCenter = _screw.HeadPoint + headPointToHeadCenterDistance * _screw.Direction;
            return headCenter;
        }

        public Point3d GetHeadCenterInDatabase()
        {
            return GetHeadCenterInDatabase(_screwDatabase, _screw.screwBrandType);
        }

        public static Point3d GetHeadCenterInDatabase(File3dm screwDatabase, ScrewBrandType screwBrandType)
        {
            var headPointToHeadCenterDistance = GetHeadPointToHeadCenterDistance(screwDatabase, screwBrandType);
            // headCenter corresponds to offset from headpoint along screw direction
            var headCenter = Point3d.Origin + headPointToHeadCenterDistance * -Vector3d.ZAxis;
            return headCenter;
        }

        private double GetHeadPointToHeadCenterDistance()
        {
            return GetHeadPointToHeadCenterDistance(_screwDatabase, _screw.screwBrandType);
        }

        private static double GetHeadPointToHeadCenterDistance(File3dm screwDatabase, ScrewBrandType screwBrandType)
        {
            // Get head contour
            var headContour = GetHeadCurve(screwDatabase, screwBrandType);
            // Get largest Z-coordinate
            var maxX = double.MinValue;
            const double resolution = 50; // number of points that will be checked
            double pointZ = 0;
            for (var t = headContour.Domain.Min; t < headContour.Domain.Max; t += headContour.Domain.Length / resolution)
            {
                var pointX = headContour.PointAt(t).X;
                if (!(pointX > maxX))
                {
                    continue;
                }

                pointZ = headContour.PointAt(t).Z;
                maxX = pointX;
            }

            var headPointToHeadCenterDistance = Math.Abs(pointZ);
            return headPointToHeadCenterDistance;
        }

        public static List<Curve> GetGuideHoleBooleanCurves(Point3d sphereCenter,
            double sphereRadius,
            double bottomCylinderRadius,
            double topCylinderRadius,
            double topCylinderHeight,
            double bottomCylinderHeightFromSphereContour)
        {
            Debug.Assert(topCylinderRadius >= bottomCylinderRadius);

            // Small bottom cylinder
            var chamferAngle = RhinoMath.ToRadians(45);
            var chamferWidth = topCylinderRadius - bottomCylinderRadius;
            var chamferHeight = chamferWidth * Math.Tan(chamferAngle);

            var bottomcylinderHeightFromSphereCenter = bottomCylinderHeightFromSphereContour + sphereRadius;
            var topCylinderDistanceFromSphereCenter = bottomcylinderHeightFromSphereCenter + chamferHeight;
            var bottomCylinderRightLine = new Line(sphereCenter.X + bottomCylinderRadius, 0, sphereCenter.Z, sphereCenter.X + bottomCylinderRadius, 0, sphereCenter.Z + bottomcylinderHeightFromSphereCenter);
            var bottomCylinderBottomLine = new Line(sphereCenter.X, 0, sphereCenter.Z, sphereCenter.X + bottomCylinderRadius, 0, sphereCenter.Z);

            // Large top cylinder
            var topCylinderTopLine = new Line(  sphereCenter.X + topCylinderRadius, 0, sphereCenter.Z + topCylinderDistanceFromSphereCenter + topCylinderHeight,
                                                sphereCenter.X, 0, sphereCenter.Z + topCylinderDistanceFromSphereCenter + topCylinderHeight);
            var topCylinderRightLine = new Line(sphereCenter.X + topCylinderRadius, 0, sphereCenter.Z + topCylinderDistanceFromSphereCenter,
                                                sphereCenter.X + topCylinderRadius, 0, sphereCenter.Z + topCylinderDistanceFromSphereCenter + topCylinderHeight);

            // Chamfer
            var chamferRightLine = new Line(topCylinderRightLine.From, bottomCylinderRightLine.To);

            return new List<Curve>() {  bottomCylinderBottomLine.ToNurbsCurve(),
                bottomCylinderRightLine.ToNurbsCurve(),
                chamferRightLine.ToNurbsCurve(),
                topCylinderRightLine.ToNurbsCurve(),
                topCylinderTopLine.ToNurbsCurve() };
        }

        public double GetGuideCylinderHeight(ScrewPosition screwPosition)
        {
            var cylinderCurveBuffer = screwPosition == ScrewPosition.Cup ? 
                GetScrewAideCurveGeometry(SuffixGuideCupCylinderUpperContour).ToNurbsCurve() : 
                GetScrewAideCurveGeometry(SuffixGuideFlangeCylinderUpperContour).ToNurbsCurve();

            var curvePoints = cylinderCurveBuffer.Points.Select(p => p.Location).ToList();
                    
            var ptCenterBegin = curvePoints.FirstOrDefault();
            var ptCenterEnd = curvePoints.LastOrDefault();

            var cylinderHeight = (ptCenterBegin - ptCenterEnd).Length;

            return cylinderHeight;
        }

        public Brep GetGuideHoleSafetyZone(double drillBitRadius)
        {
            Debug.Assert(drillBitRadius >= 0);

            var cupCylinderCurveBuffer = GetScrewAideCurveGeometry(SuffixGuideCupCylinderUpperContour).ToNurbsCurve();
            var flangeCylinderCurveBuffer = GetScrewAideCurveGeometry(SuffixGuideFlangeCylinderUpperContour).ToNurbsCurve();

            var curvePoints = new List<Point3d>();

            switch (_screw.positioning)
            {
                case ScrewPosition.Cup:
                    curvePoints = cupCylinderCurveBuffer.Points.Select(p => p.Location).ToList();
                    break;
                case ScrewPosition.Flange:
                    curvePoints = flangeCylinderCurveBuffer.Points.Select(p => p.Location).ToList();
                    break;
                case ScrewPosition.Any:
                    break;
                default:
                    return null;
            }

            Debug.Assert(curvePoints.Count >= 4);

            var ptCenterBegin = curvePoints.FirstOrDefault();
            var ptCenterEnd = curvePoints.LastOrDefault();

            var radius = drillBitRadius + 1;

            var safetyZoneControlPoints = new List<Point3d>
                {
                    ptCenterBegin,
                    ptCenterBegin + new Vector3d(radius, 0, 0),
                    ptCenterEnd + new Vector3d(radius, 0, 0),
                    ptCenterEnd
                };

            var guideHoleSafetyZoneCurve = new PolylineCurve(safetyZoneControlPoints);

#if (INTERNAL)

            if (ImplantDirector.IsDebugMode)
            {
                InternalUtilities.AddCurve(guideHoleSafetyZoneCurve, "guideHoleSafetyZoneCurve",
                    "Testing::guideHoleSafetyZoneCurve", Color.Magenta);
            }

#endif

            var guideHoleSafetyZoneAlignedAtZAxis = BrepUtilities.CreateEntityFromCurves(ScrewRevolveAxis, new List<Curve> { guideHoleSafetyZoneCurve });
            var guideHoleSafetyZoneAligned = GetScrewAideBrepGeometryAligned(guideHoleSafetyZoneAlignedAtZAxis);

            return guideHoleSafetyZoneAligned;
        }

        private const double GuideHoleBooleanCircleCenterDistanceUnderScrewHeadPoint = 4.5;

        public Brep GetGuideHoleBooleanBrep(double sphereRadius, double bottomCylinderRadius, double topCylinderRadius, double topCylinderHeight, double topCylinderDistanceFromSphereCenter)
        {
            var sphereCenterInDatabase = (Point3d)(ScrewAxisInDatabase * GuideHoleBooleanCircleCenterDistanceUnderScrewHeadPoint);
            var screwAideManager = new ScrewAideManager(_screw, _screwDatabase);

            var guideHoleBooleanCurves = GetGuideHoleBooleanCurves(sphereCenterInDatabase, sphereRadius, bottomCylinderRadius, topCylinderRadius, topCylinderHeight, topCylinderDistanceFromSphereCenter);
            var guideHoleBooleanAlignedAtZAxis = BrepUtilities.CreateEntityFromCurves(ScrewRevolveAxis, guideHoleBooleanCurves);
            var guideHoleBooleanAligned = screwAideManager.GetScrewAideBrepGeometryAligned(guideHoleBooleanAlignedAtZAxis);

            return guideHoleBooleanAligned;
        }

        public Brep GetGuideHoleBooleanSphereBrep(double sphereRadius)
        {
            var sphereCenterInDatabase = (Point3d)(ScrewAxisInDatabase * GuideHoleBooleanCircleCenterDistanceUnderScrewHeadPoint);
            var screwAideManager = new ScrewAideManager(_screw, _screwDatabase);

            var sphere = new Sphere(sphereCenterInDatabase, sphereRadius);

            var guideHoleBooleanSphereAligned = screwAideManager.GetScrewAideBrepGeometryAligned(sphere.ToBrep());

            return guideHoleBooleanSphereAligned;
        }

        #region Convenience functions

        public Mesh GetContainerMesh()
        {
            return GetScrewAideMeshGeometryAligned(SuffixContainerMesh);
        }
        public Mesh GetCushionBooleanMesh()
        {
            return GetScrewAideMeshGeometryAligned(SuffixCushionBooleanMesh);
        }
        public Mesh GetOutlineMesh()
        {
            return GetScrewAideMeshGeometryAligned(SuffixOutlineMesh);
        }
        public Mesh GetHorizontalBorderBumpMesh()
        {
            return GetScrewAideMeshGeometryAligned(SuffixHorizontalBorderBumpMesh);
        }
        public Mesh GetLateralBumpMesh()
        {
            return GetScrewAideMeshGeometryAligned(SuffixLateralBumpMesh);
        }
        public Mesh GetMedialBumpMesh()
        {
            return GetScrewAideMeshGeometryAligned(SuffixMedialBumpMesh);
        }
        public Mesh GetPlasticBooleanMesh()
        {
            return GetScrewAideMeshGeometryAligned(SuffixPlasticBooleanMesh);
        }
        public Mesh GetScaffoldBooleanMesh()
        {
            return GetScrewAideMeshGeometryAligned(SuffixScaffoldBooleanMesh);
        }
        public Mesh GetStudSelectorMesh()
        {
            return GetScrewAideMeshGeometryAligned(SuffixStudSelectorMesh);
        }
        public Curve GetSubtractorCurve()
        {
            return GetScrewAideCurveGeometry(SuffixSubtractorContour);
        }
        public Mesh GetSubtractorMesh()
        {
            return GetScrewAideMeshGeometryAligned(SuffixSubtractorMesh);
        }

        public static Curve GetHeadCurve(File3dm screwDatabase, ScrewBrandType screwBrandType)
        {
            return GetScrewAideCurveGeometry(screwDatabase, screwBrandType, SuffixHeadContour);
        }

        public static Curve GetHeadCalibrationCurve(File3dm screwDatabase, ScrewBrandType screwBrandType)
        {
            return GetScrewAideCurveGeometry(screwDatabase, screwBrandType, SuffixHeadCalibrationContour);
        }

        public Brep GetGuideFlangeCylinderUpperBrep()
        {
            return GetScrewAideBrepGeometryAligned(SuffixGuideFlangeCylinderUpperSurface);
        }
        public Brep GetGuideFlangeCylinderLowerBrep()
        {
            return GetScrewAideBrepGeometryAligned(SuffixGuideFlangeCylinderLowerSurface);
        }
        public Brep GetGuideCupCylinderBrep()
        {
            return GetScrewAideBrepGeometryAligned(SuffixGuideCupCylinderSurface);
        }

        #endregion
    }
}
