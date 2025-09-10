namespace IDS.CMFImplantCreation.Configurations
{
    public static class PastilleKeyNames
    {
        //IntersectionCurveComponentResult
        public const string CreationAlgoPrimaryMethod = "Primary";
        public const string CreationAlgoSecondaryMethod = "Secondary";
        public const string CylinderResult = "Cylinder";
        public const string CylinderExtrudeResult = "CylinderExtrude";
        public const string SphereResult = "Sphere";
        public const string SphereExtrudeResult = "SphereExtrude";
        public const string IntersectionCurveResult = "IntersectionCurve";

        //ExtrusionComponentResult
        public const string ExtrudeIntersectionCurveResult = "ExtrudeIntersectionCurve";
        public const string ExtrusionResult = "Extrusion";

        //PatchComponentResult and Offset
        public const string ConnectionSurfaceResult = "ConnectionSurface";
        public const string OffsetTopResult = "OffsetTop";
        public const string OffsetBottomResult = "OffsetBottom";

        //SolidMeshComponentResult
        public const string TopSolidMeshResult = "TopSolidMesh";
        public const string BottomSolidMeshResult = "BottomSolidMesh";
        public const string StitchedSolidMeshResult = "StitchedSolidMesh";
        public const string OffsetSolidMeshResult = "OffsetSolidMesh";

        //StitchMeshComponentResult
        public const string StitchedStitchMeshResult = "StitchedStitchMesh";
        public const string OffsetStitchMeshResult = "OffsetStitchMesh";

        //ScrewStampImprintComponentResult
        public const string ShapeOffsetResult = "ShapeOffset";
        public const string ShapeWidthResult = "ShapeWidth";
        public const string ShapeHeightResult = "ShapeHeight";
        public const string ShapeSectionHeightRatioResult = "ShapeSectionHeightRatio";
        public const string ShapeCreationMaxPastilleThicknessResult = "ShapeCreationMaxPastilleThickness";
        public const string StampImprintResult = "StampImprint";
    }

    public static class ConnectionKeyNames
    {
        //ConnectionIntersectionCurveComponentResult
        public const string PulledCurveResult = "PulledCurve";
        public const string TubeResult = "Tube";
        public const string IntersectionCurveResult = "ConnectionIntersectionCurve";

        // GenerateConnectionComponentResult
        public const string SharpCurvesResult = "SharpCurves";
        public const string ConnectionMeshResult = "ConnectionMesh";

        // ConnectionComponentResult
        public const string SharpConnectionMeshResult = "SharpConnectionMesh";
    }

    public static class LandmarkKeyNames
    {
        public const string LandmarkBaseMeshResult = "LandmarkBaseMesh";
        public const string IntersectionBaseCurveResult = "IntersectionBaseCurve";
        public const string LandmarkMeshResult = "LandmarkMesh";
        public const string IntersectionLandmarkCurveResult = "IntersectionLandmarkCurve";
        public const string LandmarkExtrusionResult = "LandmarkExtrusion";
        public const string LandmarkPatchSurfaceResult = "LandmarkPatchSurface";
        public const string LandmarkOffsetTopResult = "LandmarkOffsetTop";
        public const string LandmarkOffsetBottomResult = "LandmarkOffsetBottom";
        public const string LandmarkScaledUpMeshResult = "LandmarkScaledUpMesh";
        public const string LandmarkTopSolidMeshResult = "LandmarkTopSolidMesh";
        public const string LandmarkBottomSolidMeshResult = "LandmarkBottomSolidMesh";
        public const string LandmarkStitchedSolidMeshResult = "LandmarkStitchedSolidMesh";
        public const string LandmarkOffsetSolidMeshResult = "LandmarkOffsetSolidMesh";
        public const string LandmarkWrapOffsetResult = "LandmarkWrapOffset";
    }

    public static class ErrorUtilities
    {
        private const string ScrewPositionedWarning = "Common Causes\n - Screw positioned near edge of support." +
                    "\n - Screw positioned on highly concave/convex area of support." +
                    "\nPlease refer to the FAQ section on the IDS website for more information.";

        public const string ImplantCreationErrorCurveNotClosed = ScrewPositionedWarning;

        public const string ImplantCreationErrorCutoutCurveNotFound = ScrewPositionedWarning;
    }
}
