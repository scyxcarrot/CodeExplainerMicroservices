using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.Quality;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)
    [System.Runtime.InteropServices.Guid("D19E47D3-5DC2-4179-A2BE-BFC8AAA0B470")]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.Screw)]
    public class CMF_TestImplantScrewQcBubbleAndQcDoc : CmfCommandBase
    {
        private ScrewQcBubbleManager _screwQcBubblesManager;

        public CMF_TestImplantScrewQcBubbleAndQcDoc()
        {
            Instance = this;
        }

        public static CMF_TestImplantScrewQcBubbleAndQcDoc Instance { get; private set; }

        public override string EnglishName => "CMF_TestImplantScrewQcBubbleAndQcDoc";

        protected virtual string QcDocName => "Implant_Screw_Qc_Doc.html";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (_screwQcBubblesManager != null || 
                !ImplantScrewQcUtilities.PreScrewQcCheck(director))
            {
                return Result.Failure;
            }

            var screwQcCheckerManager = CreateScrewQcCheckerManager(director);

            if (screwQcCheckerManager == null)
            {
                return Result.Cancel;
            }

            // Not plan to support live update/cache for test command
            var allScrewQcResults = new Dictionary<CasePreferenceDataModel, Dictionary<Guid, KeyValuePair<QcDocBaseScrewInfoData, ImmutableList<IScrewQcResult>>>>();
            var screwManager = new ScrewManager(director);

            var casePreferences = director.CasePrefManager.CasePreferences.OrderBy(g => g.NCase);
            foreach (var casePreferenceDataModel in casePreferences)
            {
                var caseBaseScrewQcResults =
                    new Dictionary<Guid, KeyValuePair<QcDocBaseScrewInfoData, ImmutableList<IScrewQcResult>>>();
                var screws = screwManager.GetScrews(casePreferenceDataModel, false).OrderBy(s => s.Index);

                foreach (var screw in screws)
                {

                    var individualScrewInfo = QcDocScrewQueryUtilities.GetQcDocImplantScrewInfoData(director, screw, out _);

                    var individualScrewQcResults = screwQcCheckerManager.Check(screw, out _);
                    caseBaseScrewQcResults.Add(screw.Id,
                        new KeyValuePair<QcDocBaseScrewInfoData, ImmutableList<IScrewQcResult>>(individualScrewInfo,
                            individualScrewQcResults));
                }

                allScrewQcResults.Add(casePreferenceDataModel, caseBaseScrewQcResults);
            }

            ExportHtml(director, allScrewQcResults);
            CreateBubble(director, allScrewQcResults);
            return Result.Success;
        }

        protected virtual ScrewQcCheckerManager CreateScrewQcCheckerManager(CMFImplantDirector director)
        {
            PreImplantScrewQcInput preImplantScrewQcInput = null;
            return ImplantScrewQcUtilities.CreateScrewQcManager(director, ref preImplantScrewQcInput);
        }

        private void ExportHtml(CMFImplantDirector director, Dictionary<CasePreferenceDataModel, 
            Dictionary<Guid, KeyValuePair<QcDocBaseScrewInfoData, ImmutableList<IScrewQcResult>>>>results)
        {
            var resources = new CMFResources();
            string css;
            if (director.IsForUserTesting)
            {
                css = File.ReadAllText(resources.qcDocumentCssTestVersionFile);
            }
            else
            {
                css = File.ReadAllText(resources.qcDocumentCssFile);
            }

            var html = @"{
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title></title>
    <link rel=""stylesheet"" href=""https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css"" integrity=""sha384-ggOyR0iXCbMQv3Xipma34MD+dH/1fQ784/j6cY/iJTQUOhcWr7x9JvoRxT2MZw1T"" crossorigin=""anonymous"">
    <style>[CSS_STYLE]</style>
</head>
<body>
[BODY]
</body>
</html>
}";
            html = html.Replace("[CSS_STYLE]", css);
            var bodyHtml = new StringBuilder();

            foreach (var caseResult in results)
            {
                var screwQcHtml = File.ReadAllText(resources.qcDocumentImplantScrewQcDynamicScriptFile);

                var tableHtml = new StringBuilder();
                foreach (var screwResult in caseResult.Value)
                {
                    tableHtml.Append(QCScrewQcSection.GenerateImplantScrewInfoTableRow(
                        QcDocScrewQueryUtilities.GetQcDocScrewAndResultsInfoModel(screwResult.Value.Key,
                            screwResult.Value.Value)));
                }

                var implantName = caseResult.Key.CaseName;
                var implantSub = caseResult.Key.CaseName + "_sub";
                var implantSubDynamic = "dynamic_" + implantSub;

                var implantScrewQcDict = new Dictionary<string, string>()
                {
                    {ImplantScrewQcKeys.ImplantScrewInfoTableKey, tableHtml.ToString()},
                    {"IMPLANT_NAME", implantName },
                    { "DYNAMIC_IMPLANT", implantSubDynamic },
                    { "DYNAMIC_IMPLANT_SUB", implantSub }
                };

                bodyHtml.Append(QCReportUtilities.FormatFromDictionary(screwQcHtml, implantScrewQcDict));
            }

            html = html.Replace("[BODY]", bodyHtml.ToString());

            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            var qcFile = Path.Combine(workingDir, QcDocName);
            File.WriteAllText(qcFile, html);
            SystemTools.OpenExplorerInFolder(workingDir);
        }

        private void CreateBubble(CMFImplantDirector director, Dictionary<CasePreferenceDataModel,
            Dictionary<Guid, KeyValuePair<QcDocBaseScrewInfoData, ImmutableList<IScrewQcResult>>>> results)
        {
            _screwQcBubblesManager = ImplantScrewQcUtilities.CreateScrewQcBubbleManager(director, out var measurementsDisplay);

            var allResults = results.SelectMany(s => s.Value).ToDictionary(
                kv => kv.Key, kv => kv.Value.Value);

            measurementsDisplay.Update(allResults.Values);
            _screwQcBubblesManager.UpdateScrewBubbles(
                ImplantScrewQcUtilities.CreateScrewQcBubble(director, allResults.ToImmutableDictionary()));

            _screwQcBubblesManager.Show();

            Command.BeginCommand += HideOnNextCommand;
        }

        private void HideOnNextCommand(object sender, CommandEventArgs e)
        {
            if (_screwQcBubblesManager != null)
            {
                _screwQcBubblesManager.Clear();
                _screwQcBubblesManager = null;
                RhinoDoc.ActiveDoc.Views.Redraw();
            }

            Command.EndCommand -= HideOnNextCommand;
        }
    }
#endif
}
