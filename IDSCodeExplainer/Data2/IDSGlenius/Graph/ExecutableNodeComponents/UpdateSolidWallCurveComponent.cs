using System;
using System.Linq;
using System.Text;
using IDS.Glenius.ImplantBuildingBlocks;

namespace IDS.Glenius.Graph
{
    public class UpdateSolidWallCurveComponent : ExecutableNodeComponentBase
    {
        public UpdateSolidWallCurveComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {
        }

        //TODO:SRS says to delete
        public override bool Execute()
        {
            if (!objectManager.HasBuildingBlock(IBB.SolidWallCurve))
            {
                director.Graph.InvalidateGraph();
                return true;
            }

            var solidWallCurves = objectManager.GetAllBuildingBlocks(IBB.SolidWallCurve);

            foreach (var c in solidWallCurves)
            {
                director.SolidWallObjectManager.DeleteSolidWall(c.Id);
            }

            director.Graph.InvalidateGraph();
            return true;
        }
    }
}
