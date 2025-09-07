using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using IDS.Glenius.Operations;

namespace IDS.Glenius.Graph
{
    public class UpdatePlateBasePlateComponent : ExecutableNodeComponentBase
    {
        public UpdatePlateBasePlateComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {

        }

        public override bool Execute()
        {
            if (!objectManager.HasBuildingBlock(IBB.BasePlateTopContour) ||
                !objectManager.HasBuildingBlock(IBB.BasePlateBottomContour))
            {
                return false;
            }

            var topContour = objectManager.GetBuildingBlock(IBB.BasePlateTopContour).Geometry as Curve;
            var bottomContour = objectManager.GetBuildingBlock(IBB.BasePlateBottomContour).Geometry as Curve;

            BasePlateMaker maker = new BasePlateMaker();
            if(maker.CreateBasePlate(topContour, bottomContour, false))
            {
                var basePlateId = objectManager.GetBuildingBlockId(IBB.PlateBasePlate);
                objectManager.SetBuildingBlock(IBB.PlateBasePlate, maker.BasePlate, basePlateId);
                return true;
            }

            return false;
        }
    }
}
