using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using System.Collections.Generic;
using static IDS.CMF.Utilities.ScrewGaugeUtilities;


namespace IDS.CMF.Visualization
{
    public abstract class ScrewGaugeConduitBase
    {
        protected const double Transparency = 0.0;
        protected List<Screw> screws;
        protected List<ScrewGaugeConduit> screwGaugeConduits;

        protected void Reset()
        {
            screwGaugeConduits.ForEach(x => x.Enabled = false);
            screwGaugeConduits = null;
        }

        protected void CreateScrewGaugeConduit(Screw screw)
        {
            var gauges = CreateScrewGauges(screw, screw.ScrewType);
            gauges.ForEach(x => x.GaugeMaterial.Transparency = Transparency);
            var conduit = SetUpScrewGaugeConduit(gauges);
            screwGaugeConduits.Add(conduit);
        }

        protected ScrewGaugeConduit SetUpScrewGaugeConduit(List<ScrewGaugeUtilities.ScrewGaugeData> gauges)
        {
            return new ScrewGaugeConduit(gauges)
            {
                ShowScrew = true,
                ShowGaugeOutline = false,
                ShowScrewOutline = false,
                Enabled = true
            };
        }
    }
}
