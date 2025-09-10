using System.Collections.Generic;

namespace IDS.CMFImplantCreation.Configurations
{
    public class PastilleConfigurationList
    {
        public List<PastilleConfiguration> Pastilles { get; set; }
    }
    
    public class PastilleConfiguration
    {
        public string ScrewType { get; set; }
        public double StampImprintShapeOffset { get; set; }
        public double StampImprintShapeWidth { get; set; }
        public double StampImprintShapeHeight { get; set; }
        public double StampImprintShapeSectionHeightRatio { get; set; }
        public double StampImprintShapeCreationMaxPastilleThickness { get; set; }
    }
}
