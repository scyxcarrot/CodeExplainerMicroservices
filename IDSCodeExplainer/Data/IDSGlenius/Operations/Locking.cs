using IDS.Core.ImplantBuildingBlocks;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Operations
{
    public class Locking : Core.Operations.Locking
    {
        public static void UnlockHeadReamingEntities(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.ReamingEntity] });
        }

        public static void UnlockScaffoldReamingEntities(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.ScaffoldReamingEntity] });
        }

        public static void UnlockScrews(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.Screw] });
        }

        public static void UnlockScrewMantles(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.ScrewMantle] });
        }

        public static void UnlockDefectRegionCurves(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.DefectRegionCurves] });
        }

        public static void UnlockScaffoldBorders(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.ScaffoldPrimaryBorder], BuildingBlocks.Blocks[IBB.ScaffoldSecondaryBorder] });
        }

        public static void UnlockScaffoldGuides(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.ScaffoldGuides] });
        }

        public static void UnlockScaffoldCurves(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock>
            {
                BuildingBlocks.Blocks[IBB.BasePlateBottomContour],
                BuildingBlocks.Blocks[IBB.ScaffoldPrimaryBorder],
                BuildingBlocks.Blocks[IBB.ScaffoldGuides],
                BuildingBlocks.Blocks[IBB.ScaffoldSide]
            });
        }

        public static void UnlockSolidWallWrap(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.SolidWallWrap] });
        }

        public static void UnlockSolidWallCurve(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.SolidWallCurve] });
        }

        public static void UnlockImplantCreation(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock>
            {
                BuildingBlocks.Blocks[IBB.BasePlateTopContour],
                BuildingBlocks.Blocks[IBB.CylinderHat],
                BuildingBlocks.Blocks[IBB.BasePlateBottomContour],
                BuildingBlocks.Blocks[IBB.SolidWallWrap],
                BuildingBlocks.Blocks[IBB.ScrewMantle],
                BuildingBlocks.Blocks[IBB.ScapulaReamed],
                BuildingBlocks.Blocks[IBB.Screw],
                BuildingBlocks.Blocks[IBB.ProductionRod],
                BuildingBlocks.Blocks[IBB.M4ConnectionScrew],
                BuildingBlocks.Blocks[IBB.Head],
                BuildingBlocks.Blocks[IBB.ScaffoldTop],
                BuildingBlocks.Blocks[IBB.ScaffoldBottom],
                BuildingBlocks.Blocks[IBB.ScaffoldSide]
            });
        }

        public static void UnlockReferenceEntities(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.ReferenceEntities] });
        }
    }
}
