using Newtonsoft.Json;
using System.Collections.Generic;

namespace IDS.CMF.CasePreferences
{
    public class ScrewPreferences
    {
        [JsonProperty("ScrewType")]
        public string ScrewType { get; set; }
        public double PastilleDiameter { get; set; }
    }
    public class ImplantPreferences
    {
        [JsonProperty("ImplantType")]
        public string ImplantType { get; set; }
        public List<string> SurgicalApproach {get; set;}
        #region Plate
        public double PlateThickness { get; set; }
        public double PlateThicknessMin { get; set; }
        public double PlateThicknessMax { get; set; }
        public double PlateWidth { get; set; }
        public double PlateWidthMin { get; set; }
        public double PlateWidthMax { get; set; }
        public double LinkWidth { get; set; }
        public double LinkWidthMin { get; set; }
        public double LinkWidthMax { get; set; }
        #endregion
        public List<ScrewPreferences> Screw { get; set; }
        public List<string> ScrewFixationMain { get; set; }
        public List<string> ScrewFixationRemaining { get; set; }
        public List<string> ScrewFixationGraft { get; set; }
        public List<string> GuideScrews { get; set; }
        public List<string> GuideCutSlot { get; set; }
        public List<string> GuideConnections { get; set; }
        public double ScrewDistanceMin { get; set; }
        public string ScrewDistanceMax { get; set; }
        public double ScrewSafetyCurve { get; set; } //A diameter
    }

    public class ScrewBrandCasePreferencesInfo
    {
        [JsonProperty("ScrewBrand")]
        public string ScrewBrand { get; set; }
        public List<ImplantPreferences> Implants { get; set; }
    }

}
