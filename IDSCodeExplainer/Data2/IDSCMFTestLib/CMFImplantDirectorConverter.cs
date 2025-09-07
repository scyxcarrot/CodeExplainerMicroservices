using System.IO;
using IDS.CMF.TestLib.Components;
using IDS.CMF.TestLib.Utilities;
using Newtonsoft.Json;
using JsonUtilities = IDS.Core.V2.Utilities.JsonUtilities;

namespace IDS.CMF.TestLib
{
    public static class CMFImplantDirectorConverter
    {
        public static string Dump(CMFImplantDirector director, string workDir)
        {
            var caseConfig = new CaseConfig();
            caseConfig.FillToComponents(director, workDir);
            return JsonUtilities.Serialize(caseConfig, Formatting.Indented);
        }

        public static string DumpAllLayerAndParts(CMFImplantDirector director)
        {
            var allLayerAndParts = new AllLayerAndParts();
            allLayerAndParts.FillToComponent(director);
            return JsonUtilities.Serialize(allLayerAndParts, Formatting.Indented);
        }

        public static CMFImplantDirector ParseHeadless(string caseConfigInJson, string workDir)
        {
            var director = CMFImplantDirectorUtilities.CreateHeadlessCMFImplantDirector();
            ParseToDirector(caseConfigInJson, workDir, director);
            return director;
        }

        public static CMFImplantDirector ParseHeadlessFromFile(string caseConfigFilePath, string workDir)
        {
            var caseConfigInJson = File.ReadAllText(caseConfigFilePath);
            return ParseHeadless(caseConfigInJson, workDir);
        }

        public static void ParseToDirector(string caseConfigJson, string workDir, CMFImplantDirector director)
        {
            var caseConfig = JsonUtilities.Deserialize<CaseConfig>(caseConfigJson);
            caseConfig.ParseComponentsToDirector(director, workDir);
        }

        public static bool CanParseCaseConfig(string caseConfigInJson)
        {
            try
            {
                var caseConfig = JsonUtilities.Deserialize<CaseConfig>(caseConfigInJson);
                // TODO: more check
                return caseConfig != null;
            }
            catch
            {
                // ignored
            }

            return false;
        }
    }
}
