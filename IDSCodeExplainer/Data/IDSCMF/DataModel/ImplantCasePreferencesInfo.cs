using IDS.CMF.CasePreferences;
using System.Xml.Serialization;

namespace IDS.CMF.DataModel
{
    [XmlRootAttribute("CaseInfo", IsNullable = false)]
    public class ImplantCasePreferencesInfo
    {
        [field: XmlAttribute("Version")] 
        public string Version { get; private set; }
        public SurgeryInformationData SurgeryInformation { get; private set; }
        [field: XmlArray("CasePreferences")] 
        public CasePreferencesInfo[] Cases { get; private set; }
        [field: XmlArray("GuidePreferences")] 
        public GuidePreferencesInfo[] Guides { get; private set; }

        public ImplantCasePreferencesInfo(string version, SurgeryInformationData surgeryInformation,
            CasePreferencesInfo[] cases, GuidePreferencesInfo[] guides)
        {
            Version = version;
            SurgeryInformation = surgeryInformation;
            Cases = cases;
            Guides = guides;
        }
    }

    public class CasePreferencesInfo
    {
        public int NCase { get; private set; }
        public string CaseName { get; private set; }
        public CasePreferenceData CaseData { get; private set; }

        public CasePreferencesInfo(int nCase, string caseName, CasePreferenceData caseData)
        {
            NCase = nCase;
            CaseName = caseName;
            CaseData = caseData;
        }

    }

    public class GuidePreferencesInfo
    {
        public int NCase { get; private set; }
        public string CaseName { get; private set; }
        public GuidePreferenceData GuideData { get;private set; }

        public GuidePreferencesInfo(int nCase, string caseName, GuidePreferenceData guideData)
        {
            NCase = nCase;
            CaseName = caseName;
            GuideData = guideData;
        }
    }
}