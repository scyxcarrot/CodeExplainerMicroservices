using IDS.CMF.DataModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace IDS.CMF.Operations
{
    public class ImportExportCasePreferences
    {
        public bool ExportCasePreferences(string filename, CMFImplantDirector director)
        {
            //version
            var assembly = Assembly.GetExecutingAssembly();
            var version = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;

            //surgery information
            var surgeryInformation = director.CasePrefManager.SurgeryInformation;

            //case preferences
            var implantCases = new List<CasePreferencesInfo>();
            director.CasePrefManager.CasePreferences.ForEach(x =>
            {
                var casePref = new CasePreferencesInfo(x.NCase, x.CaseName, x.CasePrefData);
                implantCases.Add(casePref);
            });
            
            var cases = implantCases.ToArray();

            //guide preferences
            var guideCases = new List<GuidePreferencesInfo>();
            director.CasePrefManager.GuidePreferences.ForEach(x =>
            {
                var guidePref = new GuidePreferencesInfo(x.NCase, x.CaseName, x.GuidePrefData);
                guideCases.Add(guidePref);
            });

            var guides = guideCases.ToArray();

            var implantCasePref = new ImplantCasePreferencesInfo(version, surgeryInformation, cases, guides);

            var serializedJson = JsonConvert.SerializeObject(implantCasePref);
            var caseWriter = new StreamWriter(filename);
            caseWriter.Write(serializedJson);
            caseWriter.Close();
            return true;
        }

        public ImplantCasePreferencesInfo ImportCasePreferences(string filePath)
        {
            using (var input = new StreamReader(filePath))
            {
                var str = input.ReadToEnd();
                return JsonConvert.DeserializeObject<ImplantCasePreferencesInfo>(str);
            }
        }
    }
}