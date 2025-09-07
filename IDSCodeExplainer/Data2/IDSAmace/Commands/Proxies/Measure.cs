using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Visualization;


namespace IDS.Amace.Proxies
{
    public static class Measure
    {
        public static CupPositionConduit MeasurementConduit { get; set; }

        public static void RefreshConduit(bool rbvPreview, Cup cup)
        {
            if (MeasurementConduit != null && MeasurementConduit.Enabled)
            {
                MeasurementConduit.cup = cup;
                if (rbvPreview)
                {
                    Visibility.CupContralateralMeasurementRbvPreview(cup.Director.Document);
                }
                else
                {
                    Visibility.CupContralateralMeasurement(cup.Director.Document);
                }
            }
            else
            {
                if (rbvPreview)
                {
                    Visibility.CupRbvPreview(cup.Director.Document);
                }
                else
                {
                    Visibility.CupDefault(cup.Director.Document);
                }
            }
        }
    }
}