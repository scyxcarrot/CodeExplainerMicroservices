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
    [System.Runtime.InteropServices.Guid("D382AAE9-ABDF-4D1F-B6E4-40C3847824D3")]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFixationScrew)]
    public class CMF_TestGuideScrewQcBubbleAndQcDoc : CmfCommandBase
    {
        private ScrewQcBubbleManager _screwQcBubblesManager;

        public CMF_TestGuideScrewQcBubbleAndQcDoc()
        {
            Instance = this;
        }

        public static CMF_TestGuideScrewQcBubbleAndQcDoc Instance { get; private set; }

        public override string EnglishName => "CMF_TestGuideScrewQcBubbleAndQcDoc";

        protected virtual string QcDocName => "Guide_Screw_Qc_Doc.html";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (_screwQcBubblesManager != null || 
                !GuideScrewQcUtilities.PreScrewQcCheck(director))
            {
                return Result.Failure;
            }

            var screwQcCheckerManager = CreateScrewQcCheckerManager(director);

            if (screwQcCheckerManager == null)
            {
                return Result.Cancel;
            }

            var allScrewQcResults = new Dictionary<GuidePreferenceDataModel, Dictionary<Guid, KeyValuePair<QcDocBaseScrewInfoData, ImmutableList<IScrewQcResult>>>>();
            var screwManager = new ScrewManager(director);

            var guideCasePreferences = director.CasePrefManager.GuidePreferences.OrderBy(g => g.NCase);
            foreach (var guidePreferenceDataModel in guideCasePreferences)
            {
                var caseBaseScrewQcResults = new Dictionary<Guid, KeyValuePair<QcDocBaseScrewInfoData, ImmutableList<IScrewQcResult>>>();
                  var screws = screwManager.GetScrews(guidePreferenceDataModel, true).OrderBy(s => s.Index);

                foreach (var screw in screws)
                {
                    var individualScrewInfo = QcDocScrewQueryUtilities.GetQcDocGuideScrewInfoData(director, screw, out _);
                    var individualScrewQcResults = screwQcCheckerManager.Check(screw, out _);
                    caseBaseScrewQcResults.Add(screw.Id, new KeyValuePair<QcDocBaseScrewInfoData, ImmutableList<IScrewQcResult>>(individualScrewInfo, individualScrewQcResults));
                }

                allScrewQcResults.Add(guidePreferenceDataModel, caseBaseScrewQcResults);
            }

            ExportHtml(director, allScrewQcResults);
            CreateBubble(director, allScrewQcResults);
            return Result.Success;
        }

        protected virtual ScrewQcCheckerManager CreateScrewQcCheckerManager(CMFImplantDirector director)
        {
            PreGuideScrewQcInput preGuideScrewQcInput = null;
            return GuideScrewQcUtilities.CreateScrewQcManager(director, false, ref preGuideScrewQcInput);
        }

        private void ExportHtml(CMFImplantDirector director, Dictionary<GuidePreferenceDataModel, 
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
                bodyHtml.Append($"<h1>{caseResult.Key.CaseName}</h1>");
                
                var screwQcHtml = File.ReadAllText(resources.qcDocumentGuideScrewQcDynamicScriptFile);

                var tableHtml = new StringBuilder();
                foreach (var screwResult in caseResult.Value)
                {
                    tableHtml.Append(QCScrewQcSection.GenerateGuideScrewInfoTableRow(
                        QcDocScrewQueryUtilities.GetQcDocScrewAndResultsInfoModel(screwResult.Value.Key,
                            screwResult.Value.Value)));
                }

                var guideScrewQcDict = new Dictionary<string, string>()
                {
                    {GuideScrewQcKeys.GuideScrewInfoTableKey, tableHtml.ToString()}
                };

                bodyHtml.Append(QCReportUtilities.FormatFromDictionary(screwQcHtml, guideScrewQcDict));
            }

            html = html.Replace("[BODY]", bodyHtml.ToString());

            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            var qcFile = Path.Combine(workingDir, QcDocName);
            File.WriteAllText(qcFile, html);
            SystemTools.OpenExplorerInFolder(workingDir);
        }

        private void CreateBubble(CMFImplantDirector director, Dictionary<GuidePreferenceDataModel,
            Dictionary<Guid, KeyValuePair<QcDocBaseScrewInfoData, ImmutableList<IScrewQcResult>>>> results)
        {
            _screwQcBubblesManager = GuideScrewQcUtilities.CreateScrewQcBubbleManager(director);

            var allResults = results.SelectMany(s => s.Value).ToDictionary(
                kv => kv.Key, kv => kv.Value.Value);

            _screwQcBubblesManager.UpdateScrewBubbles(
                GuideScrewQcUtilities.CreateScrewQcBubble(director, allResults.ToImmutableDictionary()));

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
