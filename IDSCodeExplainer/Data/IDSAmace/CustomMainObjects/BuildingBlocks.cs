using IDS.Amace.Visualization;
using IDS.Core.ImplantBuildingBlocks;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Amace.ImplantBuildingBlocks
{
    public class BuildingBlocks
    {
        #region List of blocks

        public static Dictionary<IBB, ImplantBuildingBlock> Blocks = new Dictionary<IBB, ImplantBuildingBlock>
        {
            { IBB.Generic, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.Generic,
                                    Name = IBB.Generic.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = ImplantBuildingBlockProperties.GenericLayerName,
                                    Color = Colors.GeneralGrey
                                } },
            { IBB.CollisionEntity, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.CollisionEntity,
                                    Name = IBB.CollisionEntity.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Pre-op::Collision Entity",
                                    Color = Colors.CollisionRed,
                                    ExportName = "Collision_Entities"
                                } },
            { IBB.ContralateralFemur, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ContralateralFemur,
                                    Name = IBB.ContralateralFemur.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Pre-op::Clat Femur",
                                    Color = Colors.BoneGeneral
                                } },
            { IBB.ContralateralPelvis, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ContralateralPelvis,
                                    Name = IBB.ContralateralPelvis.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Pre-op::Clat Pelvis",
                                    Color = Colors.BoneGeneral,
                                    ExportName = "Pelvis_Contralateral"
                                } },
            { IBB.DefectFemur, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.DefectFemur,
                                    Name = IBB.DefectFemur.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Pre-op::Def Femur",
                                    Color = Colors.BoneGeneral
                                } },
            { IBB.DefectPelvis, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.DefectPelvis,
                                    Name = IBB.DefectPelvis.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Pelvis::Original",
                                    Color = Colors.BoneDefect,
                                    ExportName = "Pelvis_Original"
                                } },
            { IBB.DefectPelvisTHIBQual, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.DefectPelvisTHIBQual,
                                    Name = IBB.DefectPelvisTHIBQual.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Analysis::Bone Quality",
                                    Color = Colors.GeneralGrey
                                } },
            { IBB.DefectPelvisTHICortex, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.DefectPelvisTHICortex,
                                    Name = IBB.DefectPelvisTHICortex.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Analysis::Cortex THI",
                                    Color = Colors.GeneralGrey
                                } },
            { IBB.DefectPelvisTHIWall, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.DefectPelvisTHIWall,
                                    Name = IBB.DefectPelvisTHIWall.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Analysis::Wall THI",
                                    Color = Colors.GeneralGrey
                                } },
            { IBB.OtherContralateralParts, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.OtherContralateralParts,
                                    Name = IBB.OtherContralateralParts.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Pre-op::Other Clat Parts",
                                    Color = Colors.BoneGeneral
                                } },
            { IBB.OtherDefectParts, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.OtherDefectParts,
                                    Name = IBB.OtherDefectParts.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Pre-op::Other Def Parts",
                                    Color = Colors.BoneGeneral
                                } },
            { IBB.Sacrum, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.Sacrum,
                                    Name = IBB.Sacrum.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Pre-op::Sacrum",
                                    Color = Colors.BoneGeneral,
                                    ExportName = "Sacrum"
                                } },
            { IBB.DesignMeshDifference, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.DesignMeshDifference,
                                    Name = IBB.DesignMeshDifference.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Analysis::Design Mesh Diff",
                                    Color = Colors.GeneralGrey
                                } },
            { IBB.DesignPelvis, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.DesignPelvis,
                                    Name = IBB.DesignPelvis.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Pelvis::Design",
                                    Color = Colors.BoneDefect,
                                    ExportName = "Pelvis_Design"
                                } },
            { IBB.Cup, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.Cup,
                                    Name = IBB.Cup.ToString(),
                                    GeometryType = ObjectType.Brep,
                                    Layer = "Cup::Cup",
                                    Color = Colors.MetalCup,
                                    ExportName = "Cup"
                                } },
            { IBB.CupPorousLayer, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.CupPorousLayer,
                                    Name = IBB.CupPorousLayer.ToString(),
                                    GeometryType = ObjectType.Brep,
                                    Layer = "Cup::Porous Layer",
                                    Color = Colors.PorousOrange,
                                    ExportName = "Cup_Porous"
                                } },
            { IBB.CupRbvPreview, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.CupRbvPreview,
                                    Name = IBB.CupRbvPreview.ToString(),
                                    GeometryType = ObjectType.Brep,
                                    Layer = "Cup::Reaming preview",
                                    Color = Colors.RbvCup
                                } },
            { IBB.CupReamingEntity, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.CupReamingEntity,
                                    Name = IBB.CupReamingEntity.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Cup::Reaming Entity",
                                    Color = Colors.GeneralGrey,
                                    ExportName = "Cup_ReamingEntity"
                                } },
            { IBB.CupStuds, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.CupStuds,
                                    Name = IBB.CupStuds.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Cup::Studs",
                                    Color = Colors.MetalCupStuds,
                                    ExportName = "Cup_Studs"
                                } },
            { IBB.FilledSolidCup, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.FilledSolidCup,
                                    Name = IBB.FilledSolidCup.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Cup Filled",
                                    Color = Colors.PlateTemporary,
                                    ExportName = "Cup_Filled"
                                } },
            { IBB.LateralCupSubtractor, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.LateralCupSubtractor,
                                    Name = IBB.LateralCupSubtractor.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Cup Lateral Subtractor",
                                    Color = Colors.GeneralGrey,
                                    ExportName = "Cup_LateralSubtractor"
                                } },
            { IBB.AdditionalRbv, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.AdditionalRbv,
                                    Name = IBB.AdditionalRbv.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Reaming::RBV By Extra",
                                    Color = Colors.RbvAdditional,
                                    ExportName = "RBV_ByExtra"
                                } },
            { IBB.CupRbv, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.CupRbv,
                                    Name = IBB.CupRbv.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Reaming::RBV By Cup",
                                    Color = Colors.RbvCup,
                                    ExportName = "RBV_ByCup"
                                } },
            { IBB.CupReamedPelvis, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.CupReamedPelvis,
                                    Name = IBB.CupReamedPelvis.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Pelvis Cup Reamed",
                                    Color = Colors.BoneDefect
                                } },
            { IBB.ExtraReamingEntity, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ExtraReamingEntity,
                                    Name = IBB.ExtraReamingEntity.ToString(),
                                    GeometryType = ObjectType.Brep,
                                    Layer = "Reaming::Extra Reaming Entities",
                                    Color = Colors.ReamingEntity,
                                    ExportName = "Extra_ReamingEntity"
                                } },
            { IBB.OriginalReamedPelvis, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.OriginalReamedPelvis,
                                    Name = IBB.OriginalReamedPelvis.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Pelvis::Original Reamed",
                                    Color = Colors.BoneDefect,
                                    ExportName = "Pelvis_Original_Reamed"
                                } },
            { IBB.ReamedPelvis, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ReamedPelvis,
                                    Name = IBB.ReamedPelvis.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Pelvis::Design Reamed",
                                    Color = Colors.BoneDefect,
                                    ExportName = "Pelvis_Design_Reamed"
                                } },
            { IBB.TotalRbv, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.TotalRbv,
                                    Name = IBB.TotalRbv.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Reaming::RBV Total",
                                    Color = Colors.RbvTotal,
                                    ExportName = "RBV_Total"
                                } },
            { IBB.SkirtBoneCurve, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.SkirtBoneCurve,
                                    Name = IBB.SkirtBoneCurve.ToString(),
                                    GeometryType = ObjectType.Curve,
                                    Layer = "Skirt::Bone Curve",
                                    Color = Colors.SkirtBoneCurve
                                } },
            { IBB.SkirtCupCurve, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.SkirtCupCurve,
                                    Name = IBB.SkirtCupCurve.ToString(),
                                    GeometryType = ObjectType.Curve,
                                    Layer = "Skirt::Cup Curve",
                                    Color = Colors.SkirtCupCurve
                                } },
            { IBB.SkirtGuide, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.SkirtGuide,
                                    Name = IBB.SkirtGuide.ToString(),
                                    GeometryType = ObjectType.Curve,
                                    Layer = "Skirt::Guiding Curves",
                                    Color = Colors.SkirtGuideCurve
                                } },
             { IBB.SkirtMesh, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.SkirtMesh,
                                    Name = IBB.SkirtMesh.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Skirt::Skirt",
                                    Color = Colors.PorousOrange,
                                    ExportName = "Skirt"
                                } },
             { IBB.ScaffoldBottom, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScaffoldBottom,
                                    Name = IBB.ScaffoldBottom.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Scaffold Bottom",
                                    Color = Colors.GeneralGrey
                                } },
             { IBB.ScaffoldFinalized, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScaffoldFinalized,
                                    Name = IBB.ScaffoldFinalized.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Scaffold::Processed",
                                    Color = Colors.PorousOrange,
                                    ExportName = "Scaffold_Processed"
                                } },
             { IBB.ScaffoldSupport, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScaffoldSupport,
                                    Name = IBB.ScaffoldSupport.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Scaffold::Support",
                                    Color = Colors.ScaffoldSupport
                                } },
             { IBB.ScaffoldTop, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScaffoldTop,
                                    Name = IBB.ScaffoldTop.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Scaffold Top",
                                    Color = Colors.GeneralGrey
                                } },
            { IBB.ScaffoldVolume, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScaffoldVolume,
                                    Name = IBB.ScaffoldVolume.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Scaffold::Temp",
                                    Color = Colors.PorousOrange,
                                    ExportName = "Scaffold_Temp"
                                } },
            { IBB.WrapBottom, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.WrapBottom,
                                    Name = IBB.WrapBottom.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Wrap Bottom",
                                    Color = Colors.WrapBottom
                                } },
            { IBB.WrapScrewBump, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.WrapScrewBump,
                                    Name = IBB.WrapScrewBump.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Wrap Bump Trimming",
                                    Color = Colors.GeneralGrey
                                } },
            { IBB.WrapSunkScrew, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.WrapSunkScrew,
                                    Name = IBB.WrapSunkScrew.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Wrap Screw Positioning",
                                    Color = Colors.GeneralGrey
                                } },
            { IBB.WrapTop, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.WrapTop,
                                    Name = IBB.WrapTop.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Wrap Top",
                                    Color = Colors.WrapTop
                                } },
            { IBB.PlateBumps, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.PlateBumps,
                                    Name = IBB.PlateBumps.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Plate Bumps",
                                    Color = Colors.PlateTemporary
                                } },
            { IBB.PlateClearance, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.PlateClearance,
                                    Name = IBB.PlateClearance.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Analysis::Implant Clearance",
                                    Color = Colors.GeneralGrey
                                } },
            { IBB.PlateContourBottom, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.PlateContourBottom,
                                    Name = IBB.PlateContourBottom.ToString(),
                                    GeometryType = ObjectType.Curve,
                                    Layer = "Plate::Bottom Contour",
                                    Color = Colors.PlateBottomContour
                                } },
            { IBB.PlateContourTop, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.PlateContourTop,
                                    Name = IBB.PlateContourTop.ToString(),
                                    GeometryType = ObjectType.Curve,
                                    Layer = "Plate::Top Contour",
                                    Color = Colors.PlateTopContour
                                } },
             { IBB.PlateFlat, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.PlateFlat,
                                    Name = IBB.PlateFlat.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Plate",
                                    Color = Colors.PlateTemporary
                                } },
             { IBB.PlateHoles, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.PlateHoles,
                                    Name = IBB.PlateHoles.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Plate::Plate Holes",
                                    Color = Colors.Metal,
                                    ExportName = "Plate_Holes"
                                } },
             { IBB.PlateSmoothBumps, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.PlateSmoothBumps,
                                    Name = IBB.PlateSmoothBumps.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Plate Bumps Rounded",
                                    Color = Colors.Metal
                                } },
             { IBB.PlateSmoothHoles, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.PlateSmoothHoles,
                                    Name = IBB.PlateSmoothHoles.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Plate::Plate Holes Rounded",
                                    Color = Colors.Metal,
                                    ExportName = "Plate_Holes_Rounded"
                                } },
             { IBB.SolidPlate, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.SolidPlate,
                                    Name = IBB.SolidPlate.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Plate Flat",
                                    Color = Colors.PlateTemporary,
                                    ExportName = "PlateFlat"
                                } },
            { IBB.SolidPlateBottom, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.SolidPlateBottom,
                                    Name = IBB.SolidPlateBottom.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Plate Flat Bottom",
                                    Color = Colors.PlateTemporary,
                                    ExportName = "PlateFlat_Bottom"
                                } },
            { IBB.SolidPlateRounded, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.SolidPlateRounded,
                                    Name = IBB.SolidPlateRounded.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Plate::Plate Flat Rounded",
                                    Color = Colors.PlateTemporary,
                                    ExportName = "PlateFlat_Rounded"
                                } },
            { IBB.SolidPlateSide, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.SolidPlateSide,
                                    Name = IBB.SolidPlateSide.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Plate Flat Side",
                                    Color = Colors.PlateTemporary
                                } },
            { IBB.SolidPlateTop, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.SolidPlateTop,
                                    Name = IBB.SolidPlateTop.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::Plate Flat Top",
                                    Color = Colors.PlateTemporary
                                } },
            { IBB.LateralBump, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.LateralBump,
                                    Name = IBB.LateralBump.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Screws::Lateral Augmentations",
                                    Color = Colors.MetalScrewBump,
                                    ExportName = "Screw_BumpsLat"
                                } },
            { IBB.LateralBumpTrim, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.LateralBumpTrim,
                                    Name = IBB.LateralBumpTrim.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Screws::Lateral Augmentations (Trimmed)",
                                    Color = Colors.GeneralGrey,
                                    ExportName = "Screw_BumpsLat_Trim"
                                } },
            { IBB.MedialBump, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.MedialBump,
                                    Name = IBB.MedialBump.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Screws::Medial Augmentations",
                                    Color = Colors.MetalScrewBump,
                                    ExportName = "Screw_BumpsMed"
                                } },
            { IBB.MedialBumpTrim, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.MedialBumpTrim,
                                    Name = IBB.MedialBumpTrim.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Screws::Medial Augmentations (Trimmed)",
                                    Color = Colors.GeneralGrey,
                                    ExportName = "Screw_BumpsMed_Trim"
                                } },
             { IBB.Screw, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.Screw,
                                    Name = IBB.Screw.ToString(),
                                    GeometryType = ObjectType.Brep,
                                    Layer = "Screws::Screw",
                                    Color = Colors.MetalScrew,
                                    ExportName = "Screws"
                                } },
             { IBB.ScrewContainer, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScrewContainer,
                                    Name = IBB.ScrewContainer.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Screws::Containers",
                                    Color = Colors.ScrewContainer,
                                    ExportName = "Screw_Containers"
                                } },
             { IBB.ScrewCushionSubtractor, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScrewCushionSubtractor,
                                    Name = IBB.ScrewCushionSubtractor.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::ScrewAide Cushion Boolean",
                                    Color = Colors.GeneralGrey,
                                    ExportName = "Screw_Holes_Cushion"
                                } },
             { IBB.ScrewHoleSubtractor, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScrewHoleSubtractor,
                                    Name = IBB.ScrewHoleSubtractor.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::ScrewAide Holes Plate",
                                    Color = Colors.GeneralGrey,
                                    ExportName = "Screw_Holes_Plate"
                                } },
             { IBB.ScrewMbvSubtractor, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScrewMbvSubtractor,
                                    Name = IBB.ScrewMbvSubtractor.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::ScrewAide MBV Boolean",
                                    Color = Colors.GeneralGrey,
                                    ExportName = "Screw_Holes_Scaffold"
                                } },
             { IBB.ScrewOutlineEntity, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScrewOutlineEntity,
                                    Name = IBB.ScrewOutlineEntity.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::ScrewAide Outline Entities",
                                    Color = Colors.GeneralGrey,
                                    ExportName = "Screw_OutlineEntities"
                                } },
             { IBB.ScrewPlasticSubtractor, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScrewPlasticSubtractor,
                                    Name = IBB.ScrewPlasticSubtractor.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::ScrewAide Holes Models",
                                    Color = Colors.GeneralGrey,
                                    ExportName = "Screw_Holes_Models"
                                } },
             { IBB.ScrewStudSelector, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScrewStudSelector,
                                    Name = IBB.ScrewStudSelector.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Constructors::ScrewAide Stud Selector",
                                    Color = Colors.GeneralGrey
                                } },
             { IBB.FeaVonMises, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.FeaVonMises,
                                    Name = IBB.FeaVonMises.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Analysis::FEA::Von Mises Stress",
                                    Color = Colors.GeneralGrey
                                } },
             { IBB.FeaDisplacements, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.FeaDisplacements,
                                    Name = IBB.FeaDisplacements.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Analysis::FEA::Displacements",
                                    Color = Colors.GeneralGrey
                                } },
             { IBB.FeaBoundaryConditions, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.FeaBoundaryConditions,
                                    Name = IBB.FeaBoundaryConditions.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Analysis::FEA::Boundary Conditions"
                                } },
             { IBB.FeaLoadMesh, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.FeaLoadMesh,
                                    Name = IBB.FeaLoadMesh.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Analysis::FEA::Loaded Surface"
                                } },
            { IBB.PreopPelvis, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.PreopPelvis,
                                    Name = IBB.PreopPelvis.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Pre-op::Preop Pelvis",
                                    Color = Colors.BoneDefect,
                                    ExportName = "Pelvis_Preop"
                                } },
            { IBB.BoneGraft, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.BoneGraft,
                                    Name = IBB.BoneGraft.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Bone Grafts::Bone Grafts",
                                    Color = Colors.BoneGraft,
                                    ExportName = "BoneGraft"
                                } },
            { IBB.BoneGraftRemaining, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.BoneGraftRemaining,
                                    Name = IBB.BoneGraftRemaining.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Bone Grafts::Bone Grafts Remaining",
                                    Color = Colors.BoneGraft,
                                    ExportName = "BoneGraft_Remaining"
                                } },
            { IBB.CupRbvGraft, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.CupRbvGraft,
                                    Name = IBB.CupRbvGraft.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Reaming::RBV Graft By Cup",
                                    Color = Colors.RbvCupGraft,
                                    ExportName = "RBV_GraftByCup"
                                } },
            { IBB.AdditionalRbvGraft, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.AdditionalRbvGraft,
                                    Name = IBB.AdditionalRbvGraft.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Reaming::RBV Graft By Extra",
                                    Color = Colors.RbvAdditionalGraft,
                                    ExportName = "RBV_GraftByExtra"
                                } },
            { IBB.ROIContour, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ROIContour,
                                    Name = IBB.ROIContour.ToString(),
                                    GeometryType = ObjectType.Curve,
                                    Layer = "Plate::Region of Interest",
                                    Color = Colors.RedROI
                                } },
            { IBB.IntersectionEntity, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.IntersectionEntity,
                                    Name = IBB.IntersectionEntity.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Plate::Intersection Entity",
                                    Color = Colors.GeneralGrey
                                } },
            { IBB.TransitionPreview, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.TransitionPreview,
                                    Name = IBB.TransitionPreview.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Plate::Transition Preview",
                                    Color = Colors.TransitionPreview
                                } },
        };

        #endregion

        /// <summary>
        /// Gets all building blocks.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IBB> GetAllBuildingBlocks()
        {
            return Enum.GetValues(typeof(IBB)).Cast<IBB>();
        }
    }
}