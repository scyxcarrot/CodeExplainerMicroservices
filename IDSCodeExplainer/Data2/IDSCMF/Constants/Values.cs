namespace IDS.CMF.Constants
{
    public static class RhinoScripts
    {
        public const string SaveFile = "-_Save Version=7 _Enter";
    }

    public static class Serialization
    {
        public const string KeySerializationLabel = "SerializationLabel";
    }

    public static class CommandEnglishName
    {
        public const string CMFDoMeasurements = "CMFDoMeasurements";
        public const string CMFDeleteMeasurements = "CMFDeleteMeasurements";
        public const string CMFToggleScrewInfoBubble = "CMFToggleScrewInfoBubble";
        public const string CMFToggleConnectionInfoBubble = "CMFToggleConnectionInfoBubble";
        public const string CMFToggleGuideFixationScrewInfoBubble = "CMFToggleGuideFixationScrewInfoBubble";
        public const string CMFToggleTransparency = "CMFToggleTransparency";
        public const string CMFPlaceImplant = "CMFPlaceImplant";
        public const string CMFChangeScrewNumber = "CMFChangeScrewNumber";
        public const string CMFCreateLandmark = "CMFCreateLandmark";
        public const string CMFIndicateAnatObstacles = "CMFIndicateAnatObstacles";
        public const string CMFRemoveLandmark = "CMFRemoveLandmark";
        public const string CMFToggleScrewNumber = "CMFToggleScrewNumber";

        public const string CMFStartPlanningPhase = "CMFStartPlanningPhase";
        public const string CMFStartPlanningQCPhase = "CMFStartPlanningQCPhase";
        public const string CMFStartImplantPhase = "CMFStartImplantPhase";
        public const string CMFStartGuidePhase = "CMFStartGuidePhase";
        public const string CMFStartMetalQCPhase = "CMFStartMetalQCPhase";
        public const string CMFImportImplantSupport = "CMFImportImplantSupport";
        public const string CMFCreateImplantSupport = "CMFCreateImplantSupport";
        public const string CMFImportRecut = "CMFImportRecut";
        public const string CMFUpdatePlanning = "CMFUpdatePlanning";
        public const string CMFSmartDesign = "CMFSmartDesign";
        public const string CMFImplantPreview = "CMFImplantPreview";
        public const string CMFPastillePreview = "CMFPastillePreview";

        public const string CMFOverrideBarrelType = "CMFOverrideBarrelType";
        public const string CMFTSGTeethImpressionDepthAnalysis = "CMFTSGTeethImpressionDepthAnalysis";
        public const string CMFTSGTeethBlockThicknessAnalysis = "CMFTSGTeethBlockThicknessAnalysis";
    }

    public static class Plane
    {
        public const double Size = 50.0;
    }

    public static class Transparency
    {
        public const double Invisible = 1.0;
        public const double High = 0.75;
        public const double Medium = 0.5;
        public const double Low = 0.25;
        public const double Opaque = 0.0;
        public const double Epsilon = 0.0001;
    }

    public static class ProPlanImport
    {
        public const string PreopLayer = "Preop";
        public const string OriginalLayer = "Original";
        public const string PlannedLayer = "Planned";
        public const string ObjectPrefix = "ProPlanImport_";
    }

    public static class ScrewAide
    {
        public const string Head = "Head";
        public const string HeadRef = "HeadRef";
        public const string Container = "Container";
        public const string Stamp = "Stamp";
        public const string Eye = "Eye";
        public const string EyeShape = "EyeShape";
        public const string EyeSubtractor = "EyeSubtractor";
        public const string EyeLabelTag = "EyeLabelTag";
        public const string EyeLabelTagShape = "EyeLabelTagShape";
        public const string EyeRef = "EyeRef";
        public const string EyeLabelTagRef = "EyeLabelTagRef";
        public const string Gauges = "Gauges";
    }

    public static class BarrelAide
    {
        public const string Barrel = "Barrel";
        public const string BarrelName = "BarrelName";
        public const string BarrelShape = "BarrelShape";
        public const string BarrelSubtractor = "BarrelSubtractor";
        public const string BarrelRef = "BarrelRef";
    }

    public static class LayerName
    {
        public const string OsteotomyPlanes = "Osteotomy Planes";
        public const string Nerves = "Nerves";
        public const string OthersParts = "Others";
        public const string MeasurementsPrefix = "Measurements -";
        public const string Metal = "Metal";
        public const string Graft = "Graft";
        public const string FlangeGuidingOutline = "Flange Guiding Outline";
        public const string GuideGuidingOutlines = "Guide Guiding Outlines";
        public const string TeethSupportedGuide = "Teeth Supported Guide";
    }

    public static class TeethLayer
    {
        public const string MaxillaTeeth = "Maxilla";
        public const string MandibleTeeth = "Mandible";
    }

    public static class QCValues
    {
        public const double MinDistance = 1.00;
        public const double InsertionTrajectoryDistance = 25.0;
        public const double FloatingScrewCheckTolerance = 0.45;
    }

    public static class ImplantCreation
    {
        public const double DotMeshDistancePullTolerance = 2.0;
        public const double RoIAreaRadiusOffsetModifier = 3.0;
    }

    public static class ScrewCalibratorConstants
    {
        public const double CalibrationStepSize = 0.05;
    }

    public static class OutlineAide
    {
        public const double GuideFlangeOutlineDefaultSphereRadius = 3.0;
    }

    public static class ScrewAngulationConstants
    {
        public const double AverageNormalRadiusPastille = 2.5;
        public const double AverageNormalRadiusControlPoint = 2.0;
        public const double AverageNormalRadiusGuideFixationScrew = 2.0;
    }

    public static class GuideCreationParameters
    {
        public const double MeshingParameterMaxEdgeLength = 1.0;
        public const double MeshingParameterMinEdgeLength = 0.001;
        public const double PositiveSurfaceOffset = 1.2;
        public const double NegativeSurfaceOffset = PositiveSurfaceOffset + 0.1; //Has to be bigger than Positive and Link offset.
        public const double LinkSurfaceOffset = PositiveSurfaceOffset + 0.05; //Has to be bigger than Positive offset.
        public const double SolidSurfaceOffset = 1.0;
        public const double GuideBaseOffset = SolidSurfaceOffset + 0.3; //Has to be bigger than SolidSurfaceOffset for boolean intersection.
    }

    public static class GuideFlangeParameters
    {
        public const double DefaultHeight = 4.0;
        public const double MinHeight = 1.1;
        public const double MaxHeight = 5.0;
        public const double Rounding = 2.0;
    }

    public static class ImplantMarginParameters
    {
        public const double DefaultThickness = 1.0;
        public const double MinThickness = 0.5;
        public const double MaxThickness = 1.0;
    }

    public static class DistanceParameters
    {
        public const double Epsilon3Decimal = 0.0001;
        public const double Epsilon2Decimal = 0.001;
    }

    public static class ImplantParameters
    {
        public const double OverrideConnectionMinWidth = 1.2;
        public const double OverrideConnectionMaxWidth = 6.2;
    }

    public static class ImprintFixingParameters
    {
        public const double ComplexSharpTriangleWidthThreshold = 0.05;
        public const double ComplexSharpTriangleAngelThreshold = 5;
    }

    public static class AttributeKeys
    {
        public const string KeyIsRecut = "is_recut";
        public const string KeyIsAddedAnatomy = "is_added_anatomy";
        public const string KeyIsReplacedAnatomy = "is_replaced_anatomy";
        public const string KeyTransformationMatrix = "transformation_matrix";
        public const string KeyOsteotomyType = "osteotomy_type";
        public const string KeyOsteotomyThickness = "osteotomy_thickness";
        public const string KeyOsteotomyHandlerIdentifier = "osteotomy_handler_identifier";
        public const string KeyOsteotomyHandlerCoordinate = "osteotomy_handler_coordinate";
        public const string KeyGuideBridgeType = "guide_bridge_type";
        public const string KeyGuideBridgeGenio = "guide_bridge_genio";
        public const string KeyGuideBridgeDiameter = "guide_bridge_diameter";
        public const string KeyRegisteredBarrel = "RegisteredBarrel";
        public const string KeyConnection = "Connection";
        public const string KeyIConnections = "IConnections";
    }

    public static class ImplantSupportCreationParameters
    {
        public const double DefaultGapClosingDistanceForWrapRoI1 = 4.0;
        public const double DefaultSmallestDetailForWrapUnion = 0.2;
    }

    #region SmartDesign

    public static class SmartDesignOperations
    {
        public const string RecutLefort = "LEFORT";
        public const string RecutBSSO = "BSSO";
        public const string RecutGenio = "GENIO";
        public const string RecutSplitMax = "SPLITMAX";
    }

    public static class SmartDesignReturnCodes
    {
        public const int SuccessCode = 0;
        public const int GeneralErrorCode = 1;
        public const int UnrecognisedErrorCode = 2;
        public const int CommandError = 3;
        public const int DataReadError = 10;
        public const int DataWriteError = 20;
    }

    public static class SmartDesignStrings
    {
        public const string OperationInputFolderName = "SmartDesign";
        public const string MovementsFileName = "coordinatesystems";
        public const string OsteotomyHandlerFileName = "osteotomyhandler";
        public const string WedgeFolderName = "WedgeRemoval";
        public const string WedgeBSSOLayerName = "Wedge_BSSO";
        public const string WedgeLefortLayerName = "Wedge_Lefort";
        public const string WedgeGenioLayerName = "Wedge_Genio";
    }

    #endregion

    #region ImplantProposal
    public static class ImplantProposalOperations
    {
        public const string Genio = "Genioplasty";
    }

    #endregion

    #region ScrewStyle

    public static class ObsoletedScrewStyle
    {
        public const string MiniCrossed = "Mini Crossed";
        public const string MiniSlotted = "Mini Slotted";
        public const string MicroSlotted = "Micro Slotted";
        public const string MiniCrossedHexBarrel = "Mini Crossed Hex Barrel";
        public const string MiniSlottedHexBarrel = "Mini Slotted Hex Barrel";
        public const string MiniSlottedSelfTapping = "Mini Slotted Self-Tapping";
        public const string MiniSlottedSelfDrilling = "Mini Slotted Self-Drilling";
    }

    public static class ReplacementForObsoletedScrewStyle
    {
        public const string MiniCrossedSelfDrillingBarrel = "Mini Crossed Self-Drilling";
        public const string MiniSlottedSelfDrillingBarrel = "Mini Slotted Self-Drilling";
        public const string MiniCrossedSelfDrillingHexBarrel = "Mini Crossed Self-Drilling Hex Barrel";
        public const string MiniSlottedSelfDrillingHexBarrel = "Mini Slotted Self-Drilling Hex Barrel";
        public const string MiniCrossedSelfTappingBarrel = "Mini Crossed Self-Tapping";
        public const string MiniSlottedSelfTappingBarrel = "Mini Slotted Self-Tapping";
        public const string MiniCrossedSelfTappingHexBarrel = "Mini Crossed Self-Tapping Hex Barrel";
        public const string MiniSlottedSelfTappingHexBarrel = "Mini Slotted Self-Tapping Hex Barrel";
    }

    #endregion

    #region BarrelType

    public static class BarrelTypeName
    {
        public const string Marking = "Marking";
    }

    #endregion

    public static class BarrelAttributeKeys
    {
        public const string KeyIsGuideCreationError = "IsGuideCreationError";
    }

    public static class GuideBridgeType
    {
        public const string OctagonalBridge = "Octagonal";
        public const string HexagonalBridge = "Hexagonal";
    }
}
