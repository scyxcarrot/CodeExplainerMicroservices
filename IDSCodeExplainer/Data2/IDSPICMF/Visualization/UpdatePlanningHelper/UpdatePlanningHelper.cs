using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.V2.Loader;
using IDS.CMF.V2.Logics;
using IDS.Core.Plugin;
using IDS.Interface.Loader;
using IDS.Interface.Logic;
using IDS.Interface.Tools;
using IDS.PICMF.Forms;
using IDS.PICMF.Helper;
using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.Visualization
{
    public class UpdatePlanningHelper : IUpdatePlanningHelper
    {
        private readonly CMFImplantDirector _director;
        private readonly IDSRhinoConsole _idsConsole;
        private readonly RhinoDoc _rhinoDoc;
        private readonly RunMode _runMode;

        private List<string> selectedParts;

        public UpdatePlanningHelper(CMFImplantDirector director, IDSRhinoConsole idsConsole, RhinoDoc rhinoDoc, RunMode runMode)
        {
            _director = director;
            _idsConsole = idsConsole;
            _rhinoDoc = rhinoDoc;
            _runMode = runMode;
        }

        public LogicStatus PrepareLogicParameters(out UpdatePlanningParameters parameters)
        {
            parameters = new UpdatePlanningParameters();
            var sppcFileExtension = ".sppc";
            var mcsFileExtension = ".mcs";

            // We should allow users to pick different file types by combining the extension in the filter
            parameters.FilePath = SearchFileDialog("Please select an SPPC/MCS file", 
                $"SPPC files (*{sppcFileExtension})|*{sppcFileExtension}|MCS files (*{mcsFileExtension})|*{mcsFileExtension}", 
                new []{ sppcFileExtension, mcsFileExtension });

            if (parameters.FilePath == null)
            {
                return LogicStatus.Cancel;
            }

            parameters.Loader = GetLoader(_idsConsole, parameters.FilePath);
            var importCheckboxList = CompileUpdatePlanningList(parameters.Loader, _rhinoDoc);
            var updatePlanning = new UpdatePlanning();
            new System.Windows.Interop.WindowInteropHelper(updatePlanning).Owner = RhinoApp.MainWindowHandle();

            updatePlanning.PopulateTable(importCheckboxList);
            if (updatePlanning.ShowDialog() != true)
            {
                parameters.Loader.CleanUp();
                return LogicStatus.Cancel;
            }

            selectedParts = updatePlanning.GetSelectedItems();

            return LogicStatus.Success;
        }

        public LogicStatus ProcessLogicResult(UpdatePlanningResults result)
        {
            result.SelectedParts = selectedParts;
            return LogicStatus.Success;
        }

        private string SearchFileDialog(string title, string filterDescription, string[] filter)
        {
            var fileDialog = new OpenFileDialog()
            {
                Multiselect = false,
                Title = title,
                Filter = filterDescription,
                InitialDirectory = Environment.SpecialFolder.Desktop.ToString()
            };

            var result = fileDialog.ShowDialog();

            if (result != DialogResult.OK || !filter.Any(extension => extension == Path.GetExtension(fileDialog.FileName).ToLower()))
            {
                return null;
            }

            return fileDialog.FileName;
        }

        private static IPreopLoader GetLoader(IConsole console, string filePath)
        {
            var factory = new PreopLoaderFactory();
            return factory.GetLoader(console, filePath);
        }

        public static List<ImportCheckboxModel> CompileUpdatePlanningList(string inputFilePath, RhinoDoc doc, IDSRhinoConsole console)
        {
           var loader = GetLoader(console, inputFilePath);
           return CompileUpdatePlanningList(loader, doc);
        }

        public static List<ImportCheckboxModel> CompileUpdatePlanningList(IPreopLoader loader, RhinoDoc doc)
        {
            var finalPartInfos = GetMatchingPartsInfoWithImportJson(loader);

            // To contain utility functions that uses RhinoDoc, a helper class that resides in IDSPICMF is used
            var finalIdsName = UpdateProPlanHelper.GetMatchingStlNamesWithProPlanImportJsonFromIds(doc);

            return CompileUpdatePlanningList(finalPartInfos, finalIdsName);
        }

        private static List<Tuple<string, bool>> GetMatchingPartsInfoWithImportJson(IPreopLoader loader)
        {
            return loader.GetPartInfos();
        }

        private static List<ImportCheckboxModel> CompileUpdatePlanningList(List<Tuple<string, bool>> planningPartInfos, List<string> idsPartNames)
        {
            var sortedPlanningPartInfos = planningPartInfos.OrderBy(names => names).ToList();
            var compiledList = new List<ImportCheckboxModel>();

            foreach (var sortedPart in sortedPlanningPartInfos)
            {
                compiledList.Add(new ImportCheckboxModel
                {
                    IsImportSelected = false,
                    IsReferenceObject = sortedPart.Item2,
                    PlanningObjectName = sortedPart.Item1,
                    IdsObjectName = idsPartNames.Contains(sortedPart.Item1, StringComparer.OrdinalIgnoreCase) ? sortedPart.Item1 : ""
                });
            }

            return compiledList;
        }
    }
}
