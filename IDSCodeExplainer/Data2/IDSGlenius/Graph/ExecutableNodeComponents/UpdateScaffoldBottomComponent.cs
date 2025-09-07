using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino.Geometry;

namespace IDS.Glenius.Graph
{
    public class UpdateScaffoldBottomComponent : ExecutableNodeComponentBase
    {
        public UpdateScaffoldBottomComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {

        }

        public override bool Execute()
        {
            if (!objectManager.HasBuildingBlock(IBB.ScaffoldSupport) ||
                !objectManager.HasBuildingBlock(IBB.ScaffoldSide) ||
                !objectManager.HasBuildingBlock(IBB.ScaffoldTop) ||
                director.AnatomyMeasurements == null)
            {
                return false;
            }

            var headAlignment = new HeadAlignment(director.AnatomyMeasurements, objectManager, director.Document, director.defectIsLeft);
            var headCoordinateSystem = headAlignment.GetHeadCoordinateSystem();

            var creator = new ScaffoldCreator();
            creator.ScaffoldTop = objectManager.GetBuildingBlock(IBB.ScaffoldTop).Geometry as Mesh;
            creator.ScaffoldSupport = objectManager.GetBuildingBlock(IBB.ScaffoldSupport).Geometry as Mesh;
            creator.ScaffoldSide = objectManager.GetBuildingBlock(IBB.ScaffoldSide).Geometry as Mesh;
            if (creator.CreateBottom(director.Document, headCoordinateSystem.ZAxis))
            {
                var idScaffoldBottom = objectManager.GetBuildingBlockId(IBB.ScaffoldBottom);
                objectManager.SetBuildingBlock(IBB.ScaffoldBottom, creator.ScaffoldBottom, idScaffoldBottom);
                return true;
            }

            return false;
        }
    }
}
