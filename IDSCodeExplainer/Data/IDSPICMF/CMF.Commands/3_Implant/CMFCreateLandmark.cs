using IDS.CMF;
using IDS.CMF.AttentionPointer;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.Graph;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.V2.TreeDb.Model;
using IDS.Interface.Implant;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("F0FB00D0-4CE2-4152-805A-177A411B496C")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.Screw)]
    public class CMFCreateLandmark : CmfCommandBase
    {
        public CMFCreateLandmark()
        {
            TheCommand = this;
            VisualizationComponent = new CMFLandmarkManipulationVisualization();
            IsUseBaseCustomUndoRedo = false;
        }

        /// The one and only instance of this command
        public static CMFCreateLandmark TheCommand { get; private set; }

        /// The command name as it appears on the Rhino command line
        public override string EnglishName => CommandEnglishName.CMFCreateLandmark;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var screwManager = new ScrewManager(director);
            if (!screwManager.IsAllImplantScrewsCalibrated())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning,
                    "Implant screws not calibrated yet, " +
                    "landmark location might be incorrect!");
            }

            var go = new GetOption();
            go.SetCommandPrompt("Choose which type of landmark.");
            go.AcceptNothing(true);
            var selectedLandmarkType = LandmarkType.Rectangle;
            go.AddOptionEnumList<LandmarkType>("LandmarkType", selectedLandmarkType);

            // Get user input
            while (true)
            {
                var result = go.Get();
                if (result == GetResult.Cancel)
                {
                    return Result.Cancel;
                }

                if (result == GetResult.Nothing)
                {
                    break;
                }

                if (result == GetResult.Option)
                {
                    selectedLandmarkType = go.GetSelectedEnumValue<LandmarkType>();
                }
            }

            // Unlock screws
            Locking.UnlockScrews(director.Document);

            // Select screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select a screw to create landmark.");
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            var res = selectScrew.Get();
            if (res == GetResult.Object)
            {
                // Get selected screw
                var screw = selectScrew.Object(0).Object() as Screw;
                var pastille = GetPastille(director, screw);

                var objManager = new CMFObjectManager(director);
                var casePref = objManager.GetCasePreference(screw);

                Result result;
                var pastillePreviewId = Guid.Empty;

                if (pastille.Landmark == null)
                {
                    var operation = new CreateLandmark(pastille, selectedLandmarkType);
                    result = operation.Create();
                    if (result == Result.Success)
                    {
                        var helper = new PastillePreviewHelper(director);
                        pastillePreviewId = helper.GetPastillePreviewBuildingBlockId(casePref, pastille);

                        var newLandmark = operation.NewLandmark;
                        var undoRedo = new LandmarkUndoRedo
                        {
                            Pastille = pastille,
                            NewLandmark = newLandmark,
                            OldLandmark = pastille.Landmark,
                            DotList = casePref.ImplantDataModel.DotList,
                            algo = pastille.CreationAlgoMethod,
                            IdsDocument = director.IdsDocument
                        };
                        doc.AddCustomUndoEvent("OnUndoRedo", OnUndoRedo, undoRedo);
                        pastille.Landmark = newLandmark;
                        ImplantPastilleCreationUtilities.UpdatePastilleAlgo(casePref.ImplantDataModel.DotList, pastille.Screw.Id, DotPastille.CreationAlgoMethods[0]);
                        director.ImplantManager.AddLandmarkToDocument(newLandmark);
                    }
                }
                else
                {
                    IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Screw already have an existing landmark!");
                    result = Result.Failure;
                }

                if (result == Result.Success)
                {
                    casePref.Graph.InvalidateGraph();
                    casePref.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.Landmark }, new List<TargetNode>
                    {
                        new TargetNode
                        {
                            Guids = new List<Guid> { pastillePreviewId },
                            IBB = IBB.PastillePreview
                        }
                    });
                }

                doc.Objects.UnselectAll();
                PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(director);
                doc.Views.Redraw();
                return result;
            }

            return Result.Failure;
        }

        private DotPastille GetPastille(CMFImplantDirector director, Screw screw)
        {
            var casePreferences = director.CasePrefManager.CasePreferences;
            foreach (var casePreferenceData in casePreferences)
            {
                var implant = casePreferenceData.ImplantDataModel;
                foreach (var dot in implant.DotList)
                {
                    var pastille = dot as DotPastille;
                    if (pastille?.Screw != null && pastille.Screw.Id == screw.Id)
                    {
                        return pastille;
                    }
                }
            }
            throw new Exception("Unable to find associated pastille of selected screw!");
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
            var landmarkUndoRedo = (LandmarkUndoRedo)e.Tag;

            if (e.CreatedByRedo)
            {
                //replace with new value
                landmarkUndoRedo.Pastille.Landmark = landmarkUndoRedo.NewLandmark;
                ImplantPastilleCreationUtilities.UpdatePastilleAlgo(landmarkUndoRedo.DotList, landmarkUndoRedo.Pastille.Screw.Id, DotPastille.CreationAlgoMethods[0]);
                landmarkUndoRedo.IdsDocument.Redo();
            }
            else //Undo
            {
                //replace with old value
                ImplantPastilleCreationUtilities.UpdatePastilleAlgo(landmarkUndoRedo.DotList, landmarkUndoRedo.Pastille.Screw.Id, landmarkUndoRedo.algo);
                landmarkUndoRedo.Pastille.Landmark = landmarkUndoRedo.OldLandmark;
                landmarkUndoRedo.IdsDocument.Undo();
            }
            PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(landmarkUndoRedo.DotList);
            e.Document.AddCustomUndoEvent("OnUndoRedo", OnUndoRedo, landmarkUndoRedo);
        }
    }

    internal struct LandmarkUndoRedo
    {
        public DotPastille Pastille;
        public Landmark NewLandmark;
        public Landmark OldLandmark;
        public List<IDot> DotList;
        public string algo;
        public IDSDocument IdsDocument;
    }
}