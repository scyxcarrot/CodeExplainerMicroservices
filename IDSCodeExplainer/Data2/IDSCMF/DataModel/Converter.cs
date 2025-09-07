using IDS.CMF.V2.CasePreferences;
using IDS.Core.PluginHelper;

namespace IDS.CMF.DataModel
{
    public static class Converter
    {
        public static EScrewBrand ToEScrewBrandType(string regionTypeString)
        {
            if(regionTypeString.ToLower() == "synthes")
                return EScrewBrand.Synthes;

            if (regionTypeString.ToLower() == "mtlsstandardplus" || regionTypeString.ToLower() == "materialise standard+")
                return EScrewBrand.MtlsStandardPlus;

            if (regionTypeString.ToLower() == "synthesuscanada" || regionTypeString.ToLower() == "synthes (us/canada)")
                return EScrewBrand.SynthesUsCanada;

            throw new IDSException($"ToEScrewBrandType conversion failed, {regionTypeString} is not recognized.");
        }
    }
}
