using Newtonsoft.Json;
using System.Collections.Generic;

namespace IDS.CMF.CasePreferences
{
    public class ScrewLength
    {
        public string ScrewType { get; set; }
        public List<ScrewStyle> Styles { get; set; }
        public double DefaultOrthognathic { get; set; }
        public double StampImprintShapeOffset { get; set; }
        public double StampImprintShapeWidth { get; set; }
        public double StampImprintShapeHeight { get; set; }
        public double StampImprintShapeSectionHeightRatio { get; set; }
        public double StampImprintShapeCreationMaxPastilleThickness { get; set; }
        public double DefaultReconstruction { get; set; }
        public double DefaultForGuideFixation { get; set; }
        public double QCCylinderDiameter { get; set; }
        public double ScrewDiameter { get; set; }
        public int BbColorRed { get; set; }
        public int BbColorGreen { get; set; }
        public int BbColorBlue { get; set; }
        public double GuideVicinityClearance { get; set; }
        public double GuideVicinityClearanceHeight { get; set; }
        public Dictionary<string, string> BarrelTypesAndBarrelNames { get; set; }
    }

    public class ScrewStyle
    {
        public string Name { get; set; }
        public Dictionary<double, string> Lengths { get; set; }       
    }

    public class ScrewLengthsData
    {
        [JsonProperty("ScrewLengths")]
        public List<ScrewLength> ScrewLengths { get; set; }
    }
}
