using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius.ImplantBuildingBlocks;

namespace IDS.Glenius.Graph
{
    public class UpdateSolidWallWrapComponent : ExecutableNodeComponentBase
    {
        public UpdateSolidWallWrapComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {
        }

        //TODO:SRS says to delete
        public override bool Execute()
        {
            if (!objectManager.HasBuildingBlock(IBB.SolidWallWrap))
            {
                director.Graph.InvalidateGraph();
                return true;
            }

            var solidWallWraps = objectManager.GetAllBuildingBlocks(IBB.SolidWallWrap);

            foreach (var c in solidWallWraps)
            {
                director.SolidWallObjectManager.DeleteSolidWall(c.Id);
            }

            director.Graph.InvalidateGraph();
            return true;
        }
    }
}
