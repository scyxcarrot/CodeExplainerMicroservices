using IDS.CMF.V2.CasePreferences;

namespace IDS.CMF.V2.Logics
{
    public struct ScrewBrandSurgeryParameter
    {
        public EScrewBrand ScrewBrand { get; }
        public ESurgeryType SurgeryType { get; }

        public ScrewBrandSurgeryParameter(EScrewBrand screwBrand, ESurgeryType surgeryType)
        {
            ScrewBrand = screwBrand;
            SurgeryType = surgeryType;
        }
    }
}
