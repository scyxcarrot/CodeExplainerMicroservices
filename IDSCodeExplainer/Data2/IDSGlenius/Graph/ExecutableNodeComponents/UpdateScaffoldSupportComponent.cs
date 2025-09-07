using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Relations;
using Rhino.Geometry;
using System.Linq;

namespace IDS.Glenius.Graph
{
    public class UpdateScaffoldSupportComponent : ExecutableNodeComponentBase
    {
        public UpdateScaffoldSupportComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {
        }

        public override bool Execute()
        {
            if (!objectManager.HasBuildingBlock(IBB.ScaffoldPrimaryBorder) ||
                !objectManager.HasBuildingBlock(IBB.ScapulaDesignReamed))
            {
                return false;
            }

            Mesh scapulaDesignReamed = objectManager.GetBuildingBlock(IBB.ScapulaDesignReamed).Geometry as Mesh;
            var primaryBorder = objectManager.GetBuildingBlock(IBB.ScaffoldPrimaryBorder).Geometry as Curve;
            var secondaryBorder = objectManager.GetAllBuildingBlocks(IBB.ScaffoldSecondaryBorder).Select(x => x.Geometry as Curve).ToArray(); //Optional

            var creator = new ScaffoldCreator();
            var created = creator.CreateSupport(primaryBorder, secondaryBorder, scapulaDesignReamed);
            if (created)
            {
                var idScaffoldSupport = objectManager.GetBuildingBlockId(IBB.ScaffoldSupport);
                objectManager.SetBuildingBlock(IBB.ScaffoldSupport, creator.ScaffoldSupport, idScaffoldSupport);
            }
            else
            {
                var dependencies = new Dependencies();
                dependencies.DeleteIBBsWhenScaffoldCreationFailed(objectManager);
            }

            return created;
        }

    }
}
