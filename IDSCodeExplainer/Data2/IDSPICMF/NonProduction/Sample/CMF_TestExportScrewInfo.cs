using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Newtonsoft.Json;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("DF1E4432-113E-412D-8044-75A499882EA4")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestExportScrewInfo : CmfCommandBase
    {
        private class ScrewPropertiesJson
        {
            public double[] HeadPoint { get; }
            public double[] TipPoint { get; }

            public ScrewPropertiesJson(Screw screw)
            {
                HeadPoint = new double[3] {screw.HeadPoint.X, screw.HeadPoint.Y, screw.HeadPoint.Z};
                TipPoint = new double[3] {screw.TipPoint.X, screw.TipPoint.Y, screw.TipPoint.Z};
            }
        }

        private class ScrewInfoJson
        {
            public bool IsGuideFixation { get; }
            public string ScrewType { get; }
            public ScrewPropertiesJson ScrewProperties { get; }
            public int Index { get; }
            public int CaseIndex { get; }
            public int ScrewIndex { get; }

            public ScrewInfoJson(Screw screw, int index, int caseIndex, bool isGuideFixation)
            {
                IsGuideFixation = isGuideFixation;
                ScrewType = screw.ScrewType;
                ScrewProperties = new ScrewPropertiesJson(screw);
                Index = index;
                CaseIndex = caseIndex;
                ScrewIndex = screw.Index;
            }
        }

        static CMF_TestExportScrewInfo _instance;
        public CMF_TestExportScrewInfo()
        {
            _instance = this;
        }

        public static CMF_TestExportScrewInfo Instance => _instance;

        public override string EnglishName => "CMF_TestExportScrewInfo";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Select a folder to export the implant/guide screws information";
            var rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                return Result.Cancel;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            var screwManager = new ScrewManager(director);
            var getOption = new GetOption();
            getOption.SetCommandPrompt("Choose either want to export guide or implant screw, and press ENTER/ESC to proceed");
            var isGuideScrew = new OptionToggle(true, "Implant", "Guide");
            getOption.AddOptionToggle("ScrewBelongTo", ref isGuideScrew);
            getOption.EnableTransparentCommands(false);
            while (true)
            {
                var res = getOption.Get();
                if (res == GetResult.Cancel)
                {
                    break;
                }
            }

            var isGuideFixation = isGuideScrew.CurrentValue;
            var allScrews = screwManager.GetAllScrews(isGuideFixation);
            allScrews = screwManager.SortScrews(allScrews, isGuideFixation).ToList();
            var screwsInfo = new List<ScrewInfoJson>();
            var index = 1;

            foreach (var screw in allScrews)
            {
                var caseData = isGuideFixation
                    ? (ICaseData) screwManager.GetGuidePreferenceTheScrewBelongsTo(screw)
                    : (ICaseData) screwManager.GetImplantPreferenceTheScrewBelongsTo(screw);
                screwsInfo.Add(new ScrewInfoJson(screw, index++, caseData.NCase, isGuideFixation));
            }

            using (StreamWriter file = File.CreateText(
                $"{folderPath}\\{(isGuideFixation?"GuideFixationScrewInfo": "ImplantScrewInfo")}.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, screwsInfo);
            }

            return Result.Success;
        }
    }
#endif
}
