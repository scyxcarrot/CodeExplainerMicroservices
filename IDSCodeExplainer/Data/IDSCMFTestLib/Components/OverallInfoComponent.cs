using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IDS.CMF.TestLib.Components
{
    public class OverallInfoComponent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public EScrewBrand ScrewBrand { get; set; } = EScrewBrand.Synthes;

        [JsonConverter(typeof(StringEnumConverter))]
        public ESurgeryType SurgeryType { get; set; } = ESurgeryType.Orthognathic;

        public void ParseToDirector(CMFImplantDirector director)
        {
            director.CasePrefManager.SurgeryInformation.ScrewBrand = ScrewBrand;
            director.CasePrefManager.SurgeryInformation.SurgeryType = SurgeryType;
            director.ScrewBrandCasePreferences = CasePreferencesHelper.LoadScrewBrandCasePreferencesInfo(ScrewBrand);
            director.ScrewLengthsPreferences = CasePreferencesHelper.LoadScrewLengthData();
        }

        public void FillToComponent(CMFImplantDirector director)
        {
            ScrewBrand = director.CasePrefManager.SurgeryInformation.ScrewBrand;
            SurgeryType = director.CasePrefManager.SurgeryInformation.SurgeryType;
        }
    }
}
