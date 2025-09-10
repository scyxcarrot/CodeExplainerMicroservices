using IDS.Core.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml;

namespace IDS.Glenius.ImplantBuildingBlocks
{
    public static class BuildingBlocks
    {
        #region List of blocks

        private static object lockObject = new object();

        private static Dictionary<IBB, ImplantBuildingBlock> blocks;

        public static Dictionary<IBB, ImplantBuildingBlock> Blocks
        {
            get
            {
                if (blocks == null)
                {
                    lock (lockObject)
                    {
                        blocks = SetupBlocks();
                    }
                }
                return blocks;
            }
        }

        #endregion

        /// <summary>
        /// Gets all building blocks.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IBB> GetAllBuildingBlocks()
        {
            IEnumerable<IBB> IBBs = Enum.GetValues(typeof(IBB)).Cast<IBB>();
            return IBBs;
        }

        public static IEnumerable<IBB> GetAllPossibleNonConflictingConflictingEntities()
        {
            return new List<IBB>
            {
                IBB.ScapulaBoneFragments,
                IBB.HumerusBoneFragments,
                IBB.BoneGraft,
                IBB.ScapulaCalcifiedTissue,
                IBB.HumerusCalcifiedTissue,
                IBB.Spacer,
                IBB.ScapulaCement,
                IBB.HumerusCement,
                IBB.Liner,
                IBB.ScapulaMetalPieces,
                IBB.HumerusMetalPieces,
                IBB.SpacerRod,
                IBB.Baseplate,
                IBB.HumeralHead,
                IBB.Glenosphere,
                IBB.ScapulaScrews,
                IBB.HumerusScrews,
                IBB.CerclageWire,
                IBB.Stem
            };
        }

        public static IEnumerable<IBB> GetHeadComponents()
        {
            return new List<IBB>
            {
                IBB.TaperMantleSafetyZone,
                IBB.CylinderHat,
                IBB.ProductionRod
            };
        }

        public static IEnumerable<IBB> GetM4ConnectionScrewComponents()
        {
            return new List<IBB>
            {
                IBB.M4ConnectionScrew,
                IBB.M4ConnectionSafetyZone
            };
        }

        public static IEnumerable<IBB> GetBasePlateComponents()
        {
            return new List<IBB>
            {
                IBB.BasePlateTopContour,
                IBB.BasePlateBottomContour,
                IBB.PlateBasePlate
            };
        }

        private static Dictionary<IBB, ImplantBuildingBlock> SetupBlocks()
        {
            var colors = GetColors();

            var list = new Dictionary<IBB, ImplantBuildingBlock>
            {
                { IBB.Generic, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.Generic,
                                        Name = IBB.Generic.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = ImplantBuildingBlockProperties.GenericLayerName,
                                        Color = Colors.GeneralGrey
                                    } },
                { IBB.Scapula, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.Scapula,
                                        Name = IBB.Scapula.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scapula::Scapula",
                                        Color = colors.ContainsKey("DefectScapulaBone") ? colors["DefectScapulaBone"] : Colors.DefectScapulaBone,
                                        ExportName = "Scapula"
                                    } },
                { IBB.Humerus, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.Humerus,
                                        Name = IBB.Humerus.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Humerus::Humerus",
                                        Color = colors.ContainsKey("OtherBone") ? colors["OtherBone"] : Colors.OtherBone
                                    } },
                { IBB.ScapulaBoneFragments, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScapulaBoneFragments,
                                        Name = IBB.ScapulaBoneFragments.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scapula::Bone Fragments",
                                        Color = colors.ContainsKey("BoneFragment") ? colors["BoneFragment"] : Colors.BoneFragment
                                    } },
                { IBB.HumerusBoneFragments, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.HumerusBoneFragments,
                                        Name = IBB.HumerusBoneFragments.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Humerus::Bone Fragments",
                                        Color = colors.ContainsKey("BoneFragment") ? colors["BoneFragment"] : Colors.BoneFragment
                                    } },
                { IBB.BoneGraft, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.BoneGraft,
                                        Name = IBB.BoneGraft.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scapula::Bone Graft",
                                        Color = colors.ContainsKey("BoneGraft") ? colors["BoneGraft"] : Colors.BoneGraft
                                    } },
                { IBB.ScapulaCalcifiedTissue, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScapulaCalcifiedTissue,
                                        Name = IBB.ScapulaCalcifiedTissue.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scapula::Calcified Tissue",
                                        Color = colors.ContainsKey("CalcifiedTissue") ? colors["CalcifiedTissue"] : Colors.CalcifiedTissue
                                    } },
                { IBB.HumerusCalcifiedTissue, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.HumerusCalcifiedTissue,
                                        Name = IBB.HumerusCalcifiedTissue.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Humerus::Calcified Tissue",
                                        Color = colors.ContainsKey("CalcifiedTissue") ? colors["CalcifiedTissue"] : Colors.CalcifiedTissue
                                    } },
                { IBB.Spacer, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.Spacer,
                                        Name = IBB.Spacer.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Humerus::Spacer",
                                        Color = colors.ContainsKey("Cement") ? colors["Cement"] : Colors.Cement
                                    } },
                { IBB.ScapulaCement, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScapulaCement,
                                        Name = IBB.ScapulaCement.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scapula::Cement",
                                        Color = colors.ContainsKey("Cement") ? colors["Cement"] : Colors.Cement
                                    } },
                { IBB.HumerusCement, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.HumerusCement,
                                        Name = IBB.HumerusCement.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Humerus::Cement",
                                        Color = colors.ContainsKey("Cement") ? colors["Cement"] : Colors.Cement
                                    } },
                { IBB.Liner, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.Liner,
                                        Name = IBB.Liner.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Humerus::Liner",
                                        Color = colors.ContainsKey("Liner") ? colors["Liner"] : Colors.Liner
                                    } },
                { IBB.ScapulaMetalPieces, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScapulaMetalPieces,
                                        Name = IBB.ScapulaMetalPieces.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scapula::Metal Pieces",
                                        Color = colors.ContainsKey("Metal") ? colors["Metal"] : Colors.Metal
                                    } },
                { IBB.HumerusMetalPieces, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.HumerusMetalPieces,
                                        Name = IBB.HumerusMetalPieces.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Humerus::Metal Pieces",
                                        Color = colors.ContainsKey("Metal") ? colors["Metal"] : Colors.Metal
                                    } },
                { IBB.SpacerRod, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.SpacerRod,
                                        Name = IBB.SpacerRod.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Humerus::Spacer Rod",
                                        Color = colors.ContainsKey("Metal") ? colors["Metal"] : Colors.Metal
                                    } },
                { IBB.Baseplate, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.Baseplate,
                                        Name = IBB.Baseplate.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scapula::Baseplate",
                                        Color = colors.ContainsKey("OldBaseplate") ? colors["OldBaseplate"] : Colors.OldBaseplate
                                    } },
                { IBB.HumeralHead, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.HumeralHead,
                                        Name = IBB.HumeralHead.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Humerus::Humeral Head",
                                        Color = colors.ContainsKey("OldGlenoSphere") ? colors["OldGlenoSphere"] : Colors.OldGlenoSphere
                                    } },
                { IBB.Glenosphere, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.Glenosphere,
                                        Name = IBB.Glenosphere.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scapula::Glenosphere",
                                        Color = colors.ContainsKey("OldGlenoSphere") ? colors["OldGlenoSphere"] : Colors.OldGlenoSphere
                                    } },
                { IBB.ScapulaScrews, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScapulaScrews,
                                        Name = IBB.ScapulaScrews.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scapula::Screws",
                                        Color = colors.ContainsKey("OldScrew") ? colors["OldScrew"] : Colors.OldScrew
                                    } },
                { IBB.HumerusScrews, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.HumerusScrews,
                                        Name = IBB.HumerusScrews.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Humerus::Screws",
                                        Color = colors.ContainsKey("OldScrew") ? colors["OldScrew"] : Colors.OldScrew
                                    } },
                { IBB.CerclageWire, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.CerclageWire,
                                        Name = IBB.CerclageWire.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Humerus::Cerclage Wire",
                                        Color = colors.ContainsKey("Wire") ? colors["Wire"] : Colors.Wire
                                    } },
                { IBB.Stem, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.Stem,
                                        Name = IBB.Stem.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Humerus::Stem",
                                        Color = colors.ContainsKey("Stem") ? colors["Stem"] : Colors.Stem
                                    } },
            { IBB.DefectRegionCurves, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.DefectRegionCurves,
                                    Name = IBB.DefectRegionCurves.ToString(),
                                    GeometryType = ObjectType.Curve,
                                    Layer = "Reconstruction::DefectRegionCurves",
                                    Color = Colors.DefectRegionCurve
                                } },
            { IBB.ScapulaDefectRegionRemoved, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ScapulaDefectRegionRemoved,
                                    Name = IBB.ScapulaDefectRegionRemoved.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Reconstruction::ScapulaDefectRegionRemoved",
                                    Color = colors.ContainsKey("DefectRegionRemoved") ? colors["DefectRegionRemoved"] : Colors.DefectRegionRemoved,
                                    ExportName = "Scapula_Normal"
                                } },
            { IBB.ReconstructedScapulaBone, new ImplantBuildingBlock
                                {
                                    ID = (int)IBB.ReconstructedScapulaBone,
                                    Name = IBB.ReconstructedScapulaBone.ToString(),
                                    GeometryType = ObjectType.Mesh,
                                    Layer = "Reconstruction::ScapulaReconstructed",
                                    Color = colors.ContainsKey("ScapulaReconstructed") ? colors["ScapulaReconstructed"] : Colors.ScapulaReconstructed,
                                    ExportName = "Reconstruction"
                                } },
            { IBB.NonConflictingEntities, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.NonConflictingEntities,
                                        Name = IBB.NonConflictingEntities.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Conflicting Entities::Non-Conflicting Entities",
                                        Color = colors.ContainsKey("NonConflictingEntities") ? colors["NonConflictingEntities"] : Colors.NonConflictingEntities,
                                        ExportName = "Non-Conflicting_Entities"
                                    } },
            { IBB.ConflictingEntities, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ConflictingEntities,
                                        Name = IBB.ConflictingEntities.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Conflicting Entities::Conflicting Entities",
                                        Color = colors.ContainsKey("ConflictingEntities") ? colors["ConflictingEntities"] : Colors.ConflictingEntities,
                                        ExportName = "Conflicting_Entities"
                                    } },
            { IBB.Head, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.Head,
                                        Name = IBB.Head.ToString(),
                                        GeometryType = ObjectType.Brep,
                                        Layer = "Head::Head",
                                        Color = colors.ContainsKey("Glenosphere") ? colors["Glenosphere"] : Colors.Glenosphere,
                                        ExportName = "Head"
                                    } },
            { IBB.TaperMantleSafetyZone, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.TaperMantleSafetyZone,
                                        Name = IBB.TaperMantleSafetyZone.ToString(),
                                        GeometryType = ObjectType.Brep,
                                        Layer = "Head::Taper Mantle Safety Zone",
                                        Color = colors.ContainsKey("TaperMantleSafetyZone") ? colors["TaperMantleSafetyZone"] : Colors.TaperMantleSafetyZone,
                                        ExportName = "Taper_Safety"
                                    } },
            { IBB.CylinderHat, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.CylinderHat,
                                        Name = IBB.CylinderHat.ToString(),
                                        GeometryType = ObjectType.Brep,
                                        Layer = "Head::Cylinder",
                                        Color = colors.ContainsKey("CylinderHat") ? colors["CylinderHat"] : Colors.CylinderHat,
                                        ExportName = "Cylinder"
                                    } },
            { IBB.ProductionRod, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ProductionRod,
                                        Name = IBB.ProductionRod.ToString(),
                                        GeometryType = ObjectType.Brep,
                                        Layer = "Head::Production Rod",
                                        Color = colors.ContainsKey("ProductionRod") ? colors["ProductionRod"] : Colors.ProductionRod,
                                        ExportName = "Production_Rod"
                                    } },
            { IBB.ReamingEntity, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ReamingEntity,
                                        Name = IBB.ReamingEntity.ToString(),
                                        GeometryType = ObjectType.Brep,
                                        Layer = "Reaming::Head Reaming Entities",
                                        Color = Colors.ReamingEntity,
                                    } },
            { IBB.ScapulaDesignReamed, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScapulaDesignReamed,
                                        Name = IBB.ScapulaDesignReamed.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scapula::ScapulaDesignReamed",
                                        Color = Colors.DefectScapulaBone,
                                    } },
            { IBB.RBVHead, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.RBVHead,
                                        Name = IBB.RBVHead.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Reaming::RBV Head",
                                        Color = colors.ContainsKey("RBVHead") ? colors["RBVHead"] : Colors.RbvHead,
                                        ExportName = "RBV_Head"
                                    } },
            { IBB.M4ConnectionScrew, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.M4ConnectionScrew,
                                        Name = IBB.M4ConnectionScrew.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Connection Screw::M4 Connection Screw",
                                        Color = colors.ContainsKey("ConnectionScrew") ? colors["ConnectionScrew"] : Colors.ConnectionScrew,
                                        ExportName = "M4_Screw"
                                    } },
            { IBB.M4ConnectionSafetyZone, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.M4ConnectionSafetyZone,
                                        Name = IBB.M4ConnectionSafetyZone.ToString(),
                                        GeometryType = ObjectType.Brep,
                                        Layer = "Connection Screw::M4 Connection Safety Zone",
                                        Color = colors.ContainsKey("ConnectionScrewSafetyZone") ? colors["ConnectionScrewSafetyZone"] : Colors.ConnectionScrewSafetyZone,
                                        ExportName = "M4_Safety"
                                    } },
            { IBB.Screw, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.Screw,
                                        Name = IBB.Screw.ToString(),
                                        GeometryType = ObjectType.Brep,
                                        Layer = "Screws::Screws",
                                        Color = colors.ContainsKey("Screw") ? colors["Screw"] : Colors.Screw,
                                        ExportName = "Screw{0}"
                                    } },
            { IBB.ScrewMantle, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScrewMantle,
                                        Name = IBB.ScrewMantle.ToString(),
                                        GeometryType = ObjectType.Brep,
                                        Layer = "Screws::Screw Mantles",
                                        Color = colors.ContainsKey("ScrewMantle") ? colors["ScrewMantle"] : Colors.ScrewMantle,
                                        ExportName = "Screw_Mantles"
                                    } },
            { IBB.ScrewSafetyZone, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScrewSafetyZone,
                                        Name = IBB.ScrewSafetyZone.ToString(),
                                        GeometryType = ObjectType.Brep,
                                        Layer = "Screws::Screw Safety Zones",
                                        Color = colors.ContainsKey("ScrewSafetyZone") ? colors["ScrewSafetyZone"] : Colors.ScrewSafetyZone,
                                        ExportName = "Screw{0}_Safety"
                                    } },
            { IBB.ScrewDrillGuideCylinder, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScrewDrillGuideCylinder,
                                        Name = IBB.ScrewDrillGuideCylinder.ToString(),
                                        GeometryType = ObjectType.Brep,
                                        Layer = "Screws::Screw Guide Cylinders",
                                        Color = colors.ContainsKey("ScrewGuideCylinder") ? colors["ScrewGuideCylinder"] : Colors.ScrewGuideCylinder,
                                        ExportName = "Screw_Guide_Drill_Cylinders"
                                    } },
            { IBB.BasePlateTopContour, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.BasePlateTopContour,
                                        Name = IBB.BasePlateTopContour.ToString(),
                                        GeometryType = ObjectType.Curve,
                                        Layer = "Plate::BasePlate Top Contour",
                                        Color = Color.FromArgb(0, 0, 255)
                                    } },
            { IBB.BasePlateBottomContour, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.BasePlateBottomContour,
                                        Name = IBB.BasePlateBottomContour.ToString(),
                                        GeometryType = ObjectType.Curve,
                                        Layer = "Plate::BasePlate Bottom Contour",
                                        Color = Color.FromArgb(255, 20, 147)
                                    } },
            { IBB.PlateBasePlate, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.PlateBasePlate,
                                        Name = IBB.PlateBasePlate.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Plate::BasePlate",
                                        Color = Colors.MobelifeBlue
                                    } },
            { IBB.ScaffoldPrimaryBorder, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScaffoldPrimaryBorder,
                                        Name = IBB.ScaffoldPrimaryBorder.ToString(),
                                        GeometryType = ObjectType.Curve,
                                        Layer = "Scaffold::Primary Border",
                                        Color = Color.FromArgb(255, 20, 147)
                                    } },
            { IBB.ScaffoldSecondaryBorder, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScaffoldSecondaryBorder,
                                        Name = IBB.ScaffoldSecondaryBorder.ToString(),
                                        GeometryType = ObjectType.Curve,
                                        Layer = "Scaffold::Secondary Border",
                                        Color = Color.FromArgb(255, 20, 147)
                                    } },
            { IBB.ScaffoldSupport, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScaffoldSupport,
                                        Name = IBB.ScaffoldSupport.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scaffold::Scaffold Support",
                                        Color = colors.ContainsKey("Scaffold") ? colors["Scaffold"] : Colors.Scaffold
                                    } },
             { IBB.ScaffoldTop, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScaffoldTop,
                                        Name = IBB.ScaffoldTop.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scaffold::Scaffold Top",
                                        Color = colors.ContainsKey("Scaffold") ? colors["Scaffold"] : Colors.Scaffold
                                    } },
             { IBB.ScaffoldSide, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScaffoldSide,
                                        Name = IBB.ScaffoldSide.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scaffold::Scaffold Side",
                                        Color = colors.ContainsKey("Scaffold") ? colors["Scaffold"] : Colors.Scaffold
                                    } },
             { IBB.ScaffoldBottom, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScaffoldBottom,
                                        Name = IBB.ScaffoldBottom.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scaffold::Scaffold Bottom",
                                        Color = colors.ContainsKey("Scaffold") ? colors["Scaffold"] : Colors.Scaffold
                                    } },
            { IBB.ScaffoldGuides, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScaffoldGuides,
                                        Name = IBB.ScaffoldGuides.ToString(),
                                        GeometryType = ObjectType.Curve,
                                        Layer = "Scaffold::ScaffoldGuides",
                                        Color = Color.FromArgb(255, 20, 147)
                                    } },
             { IBB.ScapulaDesign, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScapulaDesign,
                                        Name = IBB.ScapulaDesign.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scapula::ScapulaDesign",
                                        Color = Colors.DefectScapulaBone,
                                        ExportName = "Scapula_Design"
                                    } },
             { IBB.ScaffoldReamingEntity, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScaffoldReamingEntity,
                                        Name = IBB.ScaffoldReamingEntity.ToString(),
                                        GeometryType = ObjectType.Brep,
                                        Layer = "Reaming::Scaffold Reaming Entities",
                                        Color = Colors.ReamingEntity,
                                    } },
             { IBB.RbvScaffoldDesign, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.RbvScaffoldDesign,
                                        Name = IBB.RbvScaffoldDesign.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Reaming::RBV Scaffold Design",
                                        Color = colors.ContainsKey("RBVScaffold") ? colors["RBVScaffold"] : Colors.RbvScaffold
                                    } },
             { IBB.RbvHeadDesign, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.RbvHeadDesign,
                                        Name = IBB.RbvHeadDesign.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Reaming::RBV Head Design",
                                        Color = colors.ContainsKey("RBVHead") ? colors["RBVHead"] : Colors.RbvHead
                                    } },
            { IBB.SolidWallCurve, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.SolidWallCurve,
                                        Name = IBB.SolidWallCurve.ToString(),
                                        GeometryType = ObjectType.Curve,
                                        Layer = "Scaffold::SolidWallCurve",
                                        Color = Color.FromArgb(255, 20, 147)
                                    } },
            { IBB.SolidWallWrap, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.SolidWallWrap,
                                        Name = IBB.SolidWallWrap.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scaffold::SolidWallWrap",
                                        Color = colors.ContainsKey("SolidWallWrap") ? colors["SolidWallWrap"] : Colors.SolidWallWrap,
                                    } },
            { IBB.ScapulaReamed, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ScapulaReamed,
                                        Name = IBB.ScapulaReamed.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Scapula::ScapulaReamed",
                                        Color = Colors.DefectScapulaBone,
                                        ExportName = "Scapula_Reamed"
                                    } },
            { IBB.RbvScaffold, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.RbvScaffold,
                                        Name = IBB.RbvScaffold.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Reaming::RBV Scaffold",
                                        Color = colors.ContainsKey("RBVScaffold") ? colors["RBVScaffold"] : Colors.RbvScaffold,
                                        ExportName = "RBV_Implant"
                                    } },
            { IBB.ReferenceEntities, new ImplantBuildingBlock
                                    {
                                        ID = (int)IBB.ReferenceEntities,
                                        Name = IBB.ReferenceEntities.ToString(),
                                        GeometryType = ObjectType.Mesh,
                                        Layer = "Reference Entities::Reference Entities",
                                        Color =  Color.FromArgb(255, 0, 0)
                                    } },
        };
            return list;
        }

        private static Dictionary<string, Color> GetColors()
        {
            var resource = new Resources();
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(resource.GleniusColorsXmlFile);

            var colors = new Dictionary<string, Color>();
            var colorsNodes = xmlDocument.SelectNodes("colordefinitions/colors/color");
            foreach (XmlNode color in colorsNodes)
            {
                var colorName = color.InnerText;
                var rValue = color.Attributes.GetNamedItem("r").Value;
                var gValue = color.Attributes.GetNamedItem("g").Value;
                var bValue = color.Attributes.GetNamedItem("b").Value;
                int r, g, b;
                if (int.TryParse(rValue, out r) && int.TryParse(gValue, out g) &&
                    int.TryParse(bValue, out b) && !colors.ContainsKey(colorName))
                {
                    colors.Add(colorName, Color.FromArgb(r, g, b));
                }
            }
            return colors;
        }
    }
}