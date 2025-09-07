using IDS.CMF;
using IDS.CMF.AttentionPointer;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.Graph;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.V2.TreeDb.Model;
using IDS.Interface.Implant;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("D971F2B5-4AD9-483F-B140-EA42191A744C")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.Landmark)]
    public class CMFRemoveLandmark : CmfCommandBase
    {
        public CMFRemoveLandmark()
        {
            TheCommand = this;
            VisualizationComponent = new CMFLandmarkManipulationVisualization();
            IsUseBaseCustomUndoRedo = false;
        }

        /// The one and only instance of this command
        public static CMFRemoveLandmark TheCommand { get; private set; }

        /// The command name as it appears on the Rhino command line
        public override string EnglishName => CommandEnglishName.CMFRemoveLandmark;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            doc.Views.Redraw();

            Locking.UnlockLandmarks(director.Document);

            var selectLandmark = new GetObject();
            selectLandmark.SetCommandPrompt("Select landmark(s) to remove.");
            selectLandmark.EnablePreSelect(false, false);
            selectLandmark.EnablePostSelect(true);
            selectLandmark.AcceptNothing(true);
            selectLandmark.EnableTransparentCommands(false);

            var result = Result.Failure;

            while (true)
            {
                var res = selectLandmark.GetMultiple(0, 0);

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    break;
                }

                if (res == GetResult.Object)
                {
                    var dialogResult = Rhino.UI.Dialogs.ShowMessage(
                        "Are you sure you want to remove the selected landmark(s)?",
                        "Remove Landmark(s)?",
                        ShowMessageButton.YesNoCancel,
                        ShowMessageIcon.Exclamation);
                    if (dialogResult == ShowMessageResult.Yes)
                    {
                        var selectedLandmarks = doc.Objects.GetSelectedObjects(false, false).ToList();
                        var removed = DeleteLandmarks(director, selectedLandmarks);
                        result = removed ? Result.Success : Result.Failure;

                        // Stop user input
                        break;
                    }

                    if (dialogResult == ShowMessageResult.Cancel)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private bool DeleteLandmarks(CMFImplantDirector director, List<RhinoObject> selectedLandmarks)
        {
            var undoRedoParam = new RemoveLandmarkUndoRedo
            {
                UndoRedoList = new List<LandmarkUndoRedo>(),
                IdsDocument = director.IdsDocument
            };
            var helper = new PastillePreviewHelper(director);
            var objectManager = new CMFObjectManager(director);
            var pastillePreviewInfos = new Dictionary<CasePreferenceDataModel, List<DotPastille>>();

            foreach (var landmark in selectedLandmarks)
            {
                CasePreferenceDataModel casePreference;
                var pastille = GetPastille(director, landmark, out casePreference);
                if (pastillePreviewInfos.ContainsKey(casePreference))
                {
                    pastillePreviewInfos[casePreference].Add(pastille);
                }
                else
                {
                    pastillePreviewInfos.Add(casePreference, new List<DotPastille> { pastille });
                }

                var undoRedo = new LandmarkUndoRedo
                {
                    Pastille = pastille,
                    NewLandmark = null,
                    OldLandmark = pastille.Landmark,
                    DotList = casePreference.ImplantDataModel.DotList,
                    algo = pastille.CreationAlgoMethod
                };
                undoRedoParam.UndoRedoList.Add(undoRedo);

                director.ImplantManager.DeleteLandmark(pastille.Landmark);
                casePreference.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.Landmark }, IBB.PastillePreview);
                ImplantPastilleCreationUtilities.UpdatePastilleAlgo(casePreference.ImplantDataModel.DotList, pastille.Screw.Id, DotPastille.CreationAlgoMethods[0]);
            }

            foreach (var pastillePreviewInfo in pastillePreviewInfos)
            {
                var pastillePreviewIds = helper.GetPastillePreviewBuildingBlockIds(pastillePreviewInfo.Key, pastillePreviewInfo.Value);
                pastillePreviewInfo.Key.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.Landmark }, new List<TargetNode>
                    {
                        new TargetNode
                        {
                            Guids = pastillePreviewIds,
                            IBB = IBB.PastillePreview
                        }
                    });
            }

            director.Document.AddCustomUndoEvent("OnUndoRedo", OnUndoRedo, undoRedoParam);
            PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(director);

            return true;
        }

        private DotPastille GetPastille(CMFImplantDirector director, RhinoObject landmark, out CasePreferenceDataModel casePreference)
        {
            var casePreferences = director.CasePrefManager.CasePreferences;
            foreach (var casePreferenceData in casePreferences)
            {
                var implant = casePreferenceData.ImplantDataModel;
                foreach (var dot in implant.DotList)
                {
                    var pastille = dot as DotPastille;
                    if (pastille?.Landmark != null && pastille.Landmark.Id == landmark.Id)
                    {
                        casePreference = casePreferenceData;
                        return pastille;
                    }
                }
            }
            throw new Exception("Unable to find associated pastille of selected landmark!");
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }

        private void OnUndoRedo(object sender, CustomUndoEventArgs e)
        {
            var undoRedoParam = (RemoveLandmarkUndoRedo)e.Tag;
            var renewList = new List<List<IDot>>();

            if (e.CreatedByRedo)
            {
                //replace with new values
                foreach (var undoRedo in undoRedoParam.UndoRedoList)
                {
                    undoRedo.Pastille.Landmark = undoRedo.NewLandmark;
                    ImplantPastilleCreationUtilities.UpdatePastilleAlgo(undoRedo.DotList, undoRedo.Pastille.Screw.Id, DotPastille.CreationAlgoMethods[0]);
                    if (!renewList.Contains(undoRedo.DotList))
                    {
                        renewList.Add(undoRedo.DotList);
                    }
                }
                undoRedoParam.IdsDocument.Redo();
            }
            else //Undo
            {
                //replace with old values
                foreach (var undoRedo in undoRedoParam.UndoRedoList)
                {
                    undoRedo.Pastille.Landmark = undoRedo.OldLandmark;
                    ImplantPastilleCreationUtilities.UpdatePastilleAlgo(undoRedo.DotList, undoRedo.Pastille.Screw.Id, undoRedo.algo);
                    if (!renewList.Contains(undoRedo.DotList))
                    {
                        renewList.Add(undoRedo.DotList);
                    }
                }
                undoRedoParam.IdsDocument.Undo();
            }

            foreach (var dotList in renewList)
            {
                PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(dotList);
            }

            e.Document.AddCustomUndoEvent("OnUndoRedo", OnUndoRedo, undoRedoParam);
        }
    }

    internal struct RemoveLandmarkUndoRedo
    {
        public List<LandmarkUndoRedo> UndoRedoList;
        public IDSDocument IdsDocument;
    }
}