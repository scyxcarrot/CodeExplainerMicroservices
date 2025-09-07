/// <summary>
/// Everything related to implant building blocks
/// </summary>
namespace IDS.Glenius.ImplantBuildingBlocks
{
    /// <summary>
    /// Implant Building Block Types
    /// </summary>
    public enum IBB
    {
        Generic,
        Scapula,
        Humerus,
        ScapulaBoneFragments,
        HumerusBoneFragments,
        BoneGraft,
        ScapulaCalcifiedTissue,
        HumerusCalcifiedTissue,
        Spacer,
        ScapulaCement,
        HumerusCement,
        Liner,
        ScapulaMetalPieces,
        HumerusMetalPieces,
        SpacerRod,
        Baseplate, //A Pre-Op entity, Not the baseplate of Plate Phase
        HumeralHead,
        Glenosphere,
        ScapulaScrews,
        HumerusScrews,
        CerclageWire,
        Stem,
        DefectRegionCurves,
        ScapulaDefectRegionRemoved,
        ReconstructedScapulaBone,
        NonConflictingEntities,
        ConflictingEntities,
        Head,
        TaperMantleSafetyZone,
        CylinderHat,
        ProductionRod,
        ReamingEntity,
        ScapulaDesignReamed,
        RBVHead,
        M4ConnectionScrew,
        M4ConnectionSafetyZone,
        Screw,
        ScrewMantle,
        ScrewSafetyZone,
        ScrewDrillGuideCylinder,
        BasePlateTopContour,
        BasePlateBottomContour,
        PlateBasePlate,
        ScaffoldSupport,
        ScaffoldPrimaryBorder,
        ScaffoldSecondaryBorder,
        ScaffoldTop,
        ScaffoldSide,
        ScaffoldBottom,
        ScaffoldGuides,
        ScapulaDesign,
        ScaffoldReamingEntity,
        RbvScaffoldDesign,
        RbvHeadDesign,
        SolidWallCurve,
        SolidWallWrap,
        ScapulaReamed,
        RbvScaffold,
        ReferenceEntities
    }
}