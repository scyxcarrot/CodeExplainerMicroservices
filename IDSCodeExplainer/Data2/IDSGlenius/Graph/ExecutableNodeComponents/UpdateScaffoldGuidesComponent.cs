using System;
using System.Linq;
using System.Text;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Relations;

namespace IDS.Glenius.Graph
{
    public class UpdateScaffoldGuidesComponent : ExecutableNodeComponentBase
    {
        public UpdateScaffoldGuidesComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {

        }

        public override bool Execute()
        {
            if (!objectManager.HasBuildingBlock(IBB.ScaffoldGuides))
            {
                director.Graph.InvalidateGraph();
                return true;
            }

            var dependencies = new Dependencies();
            var success = dependencies.DeleteDisconnectedScaffoldGuides(director);
            director.Graph.InvalidateGraph();

            return success;
        }
    }
}
