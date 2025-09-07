using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Amace.Operations
{
    public class Locking : Core.Operations.Locking
    {
        // Unlock screws
        public static void UnlockScrews(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.Screw] });
        }

        // Unlock collision entities
        public static void UnlockCollisionEntities(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.CollisionEntity] });
        }

        // Unlock reaming entities
        public static void UnlockReamingEntities(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.ExtraReamingEntity] });
        }

        // Unlock entities for thickness analysis
        public static void UnlockThicknessAnalysisEntities(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.PlateFlat],
                BuildingBlocks.Blocks[IBB.DefectPelvis], BuildingBlocks.Blocks[IBB.ReamedPelvis], BuildingBlocks.Blocks[IBB.DesignPelvis] });
        }

        // Unlock pelvis
        public static void UnlockPelvis(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.ReamedPelvis],
                BuildingBlocks.Blocks[IBB.DesignPelvis], BuildingBlocks.Blocks[IBB.DefectPelvis] });
        }

        // Unlock Curves for skirt
        public static void UnlockSkirtCurves(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.SkirtCupCurve],
                BuildingBlocks.Blocks[IBB.SkirtBoneCurve], BuildingBlocks.Blocks[IBB.SkirtGuide], BuildingBlocks.Blocks[IBB.SkirtMesh] });
        }

        //
        public static void UnlockSkirtGuides(RhinoDoc doc)
        {
            ManageUnlocked(doc, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.SkirtGuide] });
        }
    }
}