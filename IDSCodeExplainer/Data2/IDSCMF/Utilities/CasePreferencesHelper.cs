using IDS.CMF.CasePreferences;
using IDS.CMF.FileSystem;
using IDS.CMF.V2.CasePreferences;
using IDS.CMF.Visualization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class CasePreferencesHelper
    {
        public static ScrewBrandCasePreferencesInfo LoadScrewBrandCasePreferencesInfo(EScrewBrand screwBrand)
        {
            var resources = new CMFResources();
            string casePreferencesFile = resources.CasePreferencesFolder + screwBrand.ToString() + ".json";
            var casePreferences = File.ReadAllText(casePreferencesFile);
            return JsonConvert.DeserializeObject<ScrewBrandCasePreferencesInfo>(casePreferences);
        }

        public static ScrewLengthsData LoadScrewLengthData()
        {
            var resources = new CMFResources();
            string casePreferenceDir = resources.CasePreferencesFolder + "\\Screws\\ScrewLength.json";
            var screwPreferences = File.ReadAllText(casePreferenceDir);
            var screwLengthsData = JsonConvert.DeserializeObject<ScrewLengthsData>(screwPreferences);
            screwLengthsData.ScrewLengths.ForEach(sl => sl.Styles.ForEach(s => s.Lengths = ReorderByKey(s.Lengths)));
            return screwLengthsData;
        }

        public static double GetAcceptableMinScrewDistance(EScrewBrand screwBrand, string implantType)
        {
            var screwBrandRegionCasePreferencesInfo = LoadScrewBrandCasePreferencesInfo(screwBrand);
            var implantPreferences = screwBrandRegionCasePreferencesInfo.Implants.First(impl => impl.ImplantType == implantType);
            return implantPreferences.ScrewDistanceMin;
        }

        public static double GetAcceptableMaxScrewDistance(EScrewBrand screwBrand, string implantType, double plateThickness, double plateWidth)
        {
            var screwBrandRegionCasePreferencesInfo = LoadScrewBrandCasePreferencesInfo(screwBrand);
            var implantPreferences = screwBrandRegionCasePreferencesInfo.Implants.First(impl => impl.ImplantType == implantType);

            var formulaStr = implantPreferences.ScrewDistanceMax;
            var variables = new Dictionary<string, double>
            {
                { "T", plateThickness },
                { "W", plateWidth },
            };

            var parser = new FormulaParser();
            var calculateFunc = parser.Parse(formulaStr, variables);
            return calculateFunc();
        }

        public static double GetAcceptableMinimumImplantScrewDistanceToOsteotomy(EScrewBrand screwBrand,
            string implantType)
        {
            var screwBrandRegionCasePreferencesInfo = LoadScrewBrandCasePreferencesInfo(screwBrand);
            var implantPreferences = screwBrandRegionCasePreferencesInfo.Implants.First(impl => impl.ImplantType == implantType);
            return implantPreferences.ScrewSafetyCurve/2;
        }

        public static Color GetColor(int index)
        {
            if (index == -1)
            {
                return Color.Yellow;
            }

            var color = Colors.Implant;
            switch (index % 5)
            {
                case 1:
                    color = Color.FromArgb(35, 74, 113);
                    break;
                case 2:
                    color = Color.FromArgb(117, 157, 157);
                    break;
                case 3:
                    color = Color.FromArgb(154, 160, 116);
                    break;
                case 4:
                    color = Color.FromArgb(186, 149, 97);
                    break;
                case 0:
                    color = Color.FromArgb(121, 94, 135);
                    break;
            }
            return color;
        }

        private static Dictionary<double, string> ReorderByKey(Dictionary<double, string> screwLengths)
        {
            return screwLengths.OrderBy(s => s.Key).ToDictionary(kpv => kpv.Key, kpv => kpv.Value);
        }
    }
}
