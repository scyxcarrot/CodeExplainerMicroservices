using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Relations;
using Rhino.Geometry;
using System.Linq;

namespace IDS.Glenius.Graph
{
    public class UpdateAllScaffoldPartsComponent : ExecutableNodeComponentBase
    {
        public UpdateAllScaffoldPartsComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {

        }

        public override bool Execute()
        {
            if (!objectManager.HasBuildingBlock(IBB.BasePlateBottomContour) ||
                !objectManager.HasBuildingBlock(IBB.ScaffoldPrimaryBorder) ||
                !objectManager.HasBuildingBlock(IBB.ScapulaDesignReamed))
            {
                return false;
            }

            Mesh scapulaDesignReamed = objectManager.GetBuildingBlock(IBB.ScapulaDesignReamed).Geometry as Mesh;
            var topCurve = objectManager.GetBuildingBlock(IBB.BasePlateBottomContour);
            var primaryBorder = objectManager.GetBuildingBlock(IBB.ScaffoldPrimaryBorder);
            var secondaryBorder = objectManager.GetAllBuildingBlocks(IBB.ScaffoldSecondaryBorder); //Optional
            var guides = objectManager.GetAllBuildingBlocks(IBB.ScaffoldGuides).ToArray(); //Optional

            var headAlignment = new HeadAlignment(director.AnatomyMeasurements, objectManager, director.Document, director.defectIsLeft);
            var headCoordinateSystem = headAlignment.GetHeadCoordinateSystem();

            var creator = new ScaffoldCreator();
            var created = creator.CreateAll(topCurve, primaryBorder, secondaryBorder.Select(x => x.Geometry as Curve).ToArray(),
                scapulaDesignReamed, director.Document, headCoordinateSystem.ZAxis, guides?.ToList());
            if (created)
            {
                var idScaffoldSupport = objectManager.GetBuildingBlockId(IBB.ScaffoldSupport);
                objectManager.SetBuildingBlock(IBB.ScaffoldSupport, creator.ScaffoldSupport, idScaffoldSupport);

                var idScaffoldTop = objectManager.GetBuildingBlockId(IBB.ScaffoldTop);
                objectManager.SetBuildingBlock(IBB.ScaffoldTop, creator.ScaffoldTop, idScaffoldTop);

                var idScaffoldSide = objectManager.GetBuildingBlockId(IBB.ScaffoldSide);
                objectManager.SetBuildingBlock(IBB.ScaffoldSide, creator.ScaffoldSide, idScaffoldSide);

                var idScaffoldBottom = objectManager.GetBuildingBlockId(IBB.ScaffoldBottom);
                objectManager.SetBuildingBlock(IBB.ScaffoldBottom, creator.ScaffoldBottom, idScaffoldBottom);
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
