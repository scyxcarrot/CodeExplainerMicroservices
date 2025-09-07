using System.Collections.Generic;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Visualization
{
    public class ImplantScrewAtOriginalScrewGaugeConduit : ScrewGaugeConduitBase, IScrewGaugeConduit
    {
        public ImplantScrewAtOriginalScrewGaugeConduit(List<Screw> screws)
        {
            base.screws = screws;
        }

        public void ToggleConduit(bool toggleOn)
        {
            if (toggleOn)
            {
                screwGaugeConduits = new List<ScrewGaugeConduit>();
                foreach (var screw in screws)
                {
                    CreateScrewGaugeConduit(screw);
                }
            }
            else
            {
                if (screwGaugeConduits != null)
                {
                    Reset();
                }
            }
        }
    }
}
