using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino.Geometry;

namespace IDS.Glenius.Graph
{
    public class UpdateScaffoldTopComponent : ExecutableNodeComponentBase
    {
        public UpdateScaffoldTopComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {
        }

        public override bool Execute()
        {
            if (!objectManager.HasBuildingBlock(IBB.BasePlateBottomContour))
            {
                return false;
            }

            var topCurve = objectManager.GetBuildingBlock(IBB.BasePlateBottomContour);

            var creator = new ScaffoldCreator();

            if (creator.CreateTop(topCurve.Geometry as Curve))
            {
                objectManager.SetBuildingBlock(IBB.ScaffoldTop, creator.ScaffoldTop, objectManager.GetBuildingBlockId(IBB.ScaffoldTop));
                return true;
            }

            return false;
        }
    }
}
