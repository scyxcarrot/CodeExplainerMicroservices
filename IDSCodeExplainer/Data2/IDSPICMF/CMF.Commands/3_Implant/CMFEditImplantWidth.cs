using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.Factory;
using IDS.CMF.Graph;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using IDS.PICMF.Forms;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("8EFA878F-89BD-46F1-ABB6-C7AB684F0232")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.PlanningImplant)]
    public class CMFEditImplantWidth : CmfCommandBase
    {
        public CMFEditImplantWidth()
        {
            TheCommand = this;
            VisualizationComponent = new CMFEditImplantWidthVisualization();
        }

        public static CMFEditImplantWidth TheCommand { get; private set; }

        public override string EnglishName => "CMFEditImplantWidth";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var gm = new GetOption();
            gm.SetCommandPrompt("Select implant to edit");
            gm.AcceptNothing(false);
            var caseId = GetCasePreferenceId();
            var casePreferenceData = director.CasePrefManager.GetCase(caseId);
            if (casePreferenceData == null)
            {
                return Result.Failure;
            }
            else if (!casePreferenceData.ImplantDataModel.ConnectionList.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Selected {casePreferenceData.CaseName} has no connection!");
                return Result.Failure;
            }

            var clonedDataModel = (ImplantDataModel)casePreferenceData.ImplantDataModel.Clone();
            var selectedConnections = SelectConnections(director, clonedDataModel);
            if (selectedConnections == null)
            {
                return Result.Cancel;
            }
            
            if (!selectedConnections.Any())
            {
                return Result.Failure;
            }

            var selectedWidth = GetWidth(selectedConnections);
            if (double.IsNaN(selectedWidth))
            {
                return Result.Cancel;
            }

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected width: {selectedWidth}");
            UpdateWidth(director, casePreferenceData, selectedConnections, selectedWidth);

            CasePreferencePanel.GetView().InvalidateUI();

            doc.ClearUndoRecords(true);
            doc.ClearRedoRecords();

            RhinoLayerUtilities.DeleteEmptyLayers(director.Document);

            doc.Views.Redraw();
            return Result.Success;
        }

        private Guid GetCasePreferenceId()
        {
            var casePreferenceId = Guid.Empty;
            var casePreferenceIdStr = string.Empty;
            var result = RhinoGet.GetString("CasePreferenceId", false, ref casePreferenceIdStr);
            if (result != Result.Success)
            {
                return casePreferenceId;
            }
            if (!Guid.TryParse(casePreferenceIdStr, out casePreferenceId))
            {
                casePreferenceId = Guid.Empty;
            }
            return casePreferenceId;
        }

        private List<List<IConnection>> SelectConnections(CMFImplantDirector director, ImplantDataModel dataModel)
        {
            var selector = new ConnectionSelectionHelper(director);
            selector.SetExistingImplant(dataModel);
            var selected = selector.Execute();
            if (!selected)
            {
                return null;
            }

            return selector.GetConnectionListResult();
        }

        private double GetWidth(List<List<IConnection>> selectedConnections)
        {
            var widthList = selectedConnections.SelectMany(c => c).Select(c => c.Width).Distinct().ToList();
            var initialWidth = ImplantParameters.OverrideConnectionMaxWidth;
            if (widthList.Count == 1)
            {
                initialWidth = widthList.First();
            }

            var plateCount = selectedConnections.Select(c => c.First()).Count(c => c is ConnectionPlate);
            var linkCount = selectedConnections.Select(c => c.First()).Count(c => c is ConnectionLink);
            var summary = $"Selected {selectedConnections.Count} connection(s):\n{plateCount} Plate(s)\n{linkCount} Link(s)";

            var dataModel = new ConnectionWidthViewModel
            {
                Minimum = ImplantParameters.OverrideConnectionMinWidth,
                Maximum = ImplantParameters.OverrideConnectionMaxWidth,
                SelectedWidth = initialWidth,
                Summary = summary
            };

            var dialog = new EditConnectionWidthDialog(dataModel)
            {
                Topmost = true
            };

            var proceed = dialog.ShowDialog();
            if (proceed != true)
            {
                return double.NaN;
            }

            var selectedWidth = Math.Round(dataModel.SelectedWidth, 2, MidpointRounding.AwayFromZero);
            return selectedWidth;
        }

        private void TryAddAllScrewFromConnection(IDot dot, ref List<Guid> editedScrews)
        {
            if (dot is DotPastille pastille)
            {
                editedScrews.Add(pastille.Screw.Id);
            }
        }

        private void UpdateWidth(CMFImplantDirector director, CasePreferenceDataModel casePreferenceData, List<List<IConnection>> selectedConnections, double selectedWidth)
        {
            var editedScrews = new List<Guid>(); 
            var actualConnections = new List<IConnection>();

            foreach (var connections in selectedConnections)
            {
                foreach (var connection in connections)
                {
                    if (connection is ConnectionPlate plate)
                    {
                        TryAddAllScrewFromConnection(plate.A, ref editedScrews);
                        TryAddAllScrewFromConnection(plate.B, ref editedScrews);
                    }
                    var actualConnection =
                        casePreferenceData.ImplantDataModel.ConnectionList.First(c =>
                            c.A.Equals(connection.A) && c.B.Equals(connection.B));
                    actualConnection.Width = selectedWidth;

                    // Set IsSynchronizable to false so that this connection will be excluded from syncing
                    actualConnection.IsSynchronizable = false;

                    actualConnections.Add(actualConnection);
                }
            }

            var helper = new ConnectionPreviewHelper(director);
            var connectionPreviewIds = helper.GetRhinoObjectIdsFromConnections(casePreferenceData, actualConnections);

            var implantComponent = new ImplantCaseComponent();
            var objectManager = new CMFObjectManager(director);
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PlanningImplant, casePreferenceData);

            var planningImplantBrepFactory = new PlanningImplantBrepFactory();
            var implant = planningImplantBrepFactory.CreateImplant(casePreferenceData.ImplantDataModel);
            var oldImplantGuid = objectManager.GetBuildingBlockId(buildingBlock);
            objectManager.SetBuildingBlock(buildingBlock, implant, oldImplantGuid);

            casePreferenceData.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.PlanningImplant }, new List<TargetNode>
                {
                    new TargetNode
                    {
                        Guids = connectionPreviewIds,
                        IBB = IBB.ConnectionPreview
                    }
                }, IBB.PastillePreview);

            RecheckMinMaxDistanceInExistingScrewQc(director, editedScrews);

            TrackingParameters.Add("CaseName", casePreferenceData.CaseName);
            TrackingParameters.Add("SelectedWidth", $"{selectedWidth}");
        }

        private void RecheckMinMaxDistanceInExistingScrewQc(CMFImplantDirector director, List<Guid> editedScrews)
        {
            if (director.ImplantScrewQcLiveUpdateHandler == null)
            {
                return;
            }

            var screwQcCheckManager =
                new ScrewQcCheckerManager(director, new[] { new MinMaxDistancesChecker(director) });
            var screwManager = new ScrewManager(director);
            var implantScrews = screwManager.GetAllScrews(false);
            var editedImplantScrews = implantScrews.Where(s => editedScrews.Contains(s.Id));
            director.ImplantScrewQcLiveUpdateHandler.RecheckCertainResult(screwQcCheckManager, editedImplantScrews);
        }
    }
}
