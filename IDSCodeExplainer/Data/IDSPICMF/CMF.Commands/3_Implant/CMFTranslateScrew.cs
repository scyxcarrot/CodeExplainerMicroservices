using IDS.CMF;
using IDS.CMF.AttentionPointer;
using IDS.CMF.CommandHelpers;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.PluginHelper;
using IDS.Core.V2.Utilities;
using IDS.PICMF.Forms;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using IDS.RhinoInterface.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("7310045B-4895-4A55-9AEE-F07F1C899850")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.Screw)]
    public class CMFTranslateScrew : CMFImplantScrewBaseCommand
    {
        public CMFTranslateScrew()
        {
            TheCommand = this;
            VisualizationComponent = new CMFManipulateImplantScrewVisualization();
            IsUseBaseCustomUndoRedo = false;
        }

        /// The one and only instance of this command
        public static CMFTranslateScrew TheCommand { get; private set; }

        /// The command name as it appears on the Rhino command line
        public override string EnglishName => "CMFTranslateScrew";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            // Get selected screw
            var screw = SelectAllScrew(doc, "Select a screw to translate.");
            if (screw != null)
            {
                var operation = new TranslateImplantScrew(screw);

                var objectManager = new CMFObjectManager(director);
                var casePreferenceData = objectManager.GetCasePreference(screw);
                var implantSupportManager = new ImplantSupportManager(objectManager);

                ImplantSurfaceRoIVisualizer RoIVisualizer = null;

                var rhSupport = implantSupportManager.GetImplantSupportRhObj(casePreferenceData);
                if (rhSupport != null)
                {
                    Mesh lowLoDSupportMesh;
                    objectManager.GetBuildingBlockLoDLow(rhSupport.Id, out lowLoDSupportMesh);
                    operation.LowLoDSupportMesh = lowLoDSupportMesh;

                    operation.ActualSupportRhObject = rhSupport;
                    RoIVisualizer = new ImplantSurfaceRoIVisualizer(casePreferenceData, rhSupport);
                }
                else
                {
                    var constraintMeshQuery = new ConstraintMeshQuery(objectManager);
                    var targetLowLoDMeshes = constraintMeshQuery.GetConstraintMeshesForImplant(true).ToList();
                    var duplicated = targetLowLoDMeshes.Select(mesh => RhinoMeshConverter.ToIDSMesh(mesh.DuplicateMesh()));
                    var mergedIdsMesh = MeshUtilitiesV2.AppendMeshes(duplicated);
                    var merged = RhinoMeshConverter.ToRhinoMesh(mergedIdsMesh);
                    if (merged != null && merged.FaceNormals.Count == 0)
                    {
                        merged.FaceNormals.ComputeFaceNormals();
                    }
                    operation.LowLoDSupportMesh = merged;
                }

                var implantDataModel = (ImplantDataModel)casePreferenceData.ImplantDataModel.Clone();
                operation.OldImplantDataModel = implantDataModel;

                Result result = Result.Nothing;

                try
                {
                    if (rhSupport != null)
                    {
                        RoIVisualizer = new ImplantSurfaceRoIVisualizer(casePreferenceData, rhSupport);
                        RoIVisualizer.Enabled = true;
                    }
                    result = operation.Translate();
                }
                catch (Exception e)
                {
                    Msai.TrackException(e, "CMF");
                    if (RoIVisualizer != null)
                    {
                        RoIVisualizer.Enabled = false;
                    }
                }

                if (RoIVisualizer != null)
                {
                    RoIVisualizer.Enabled = false;
                }

                RoIVisualizer?.Dispose();

                if (result == Result.Success)
                {
                    var screwId = screw.Id;
                    var newScrew = (Screw)director.Document.Objects.Find(screwId);

                    var casePreference = objectManager.GetCasePreference(screw);
                    ImplantPastilleCreationUtilities.UpdatePastilleAlgo(casePreference.ImplantDataModel.DotList, screw.Id, DotPastille.CreationAlgoMethods[0]);
                    var newImplantDataModel = (ImplantDataModel)casePreferenceData.ImplantDataModel.Clone();

                    var screwQcUndoRedo = new ScrewQcUndoRedo
                    {
                        NewScrew = newScrew,
                        OldScrew = screw,
                        NewImplantDataModel = newImplantDataModel,
                        OldImplantDataModel = implantDataModel,
                        CasePreferenceDataModel = casePreferenceData,
                        IdsDocument = director.IdsDocument
                    };

                    var screwUndoRedo = new ScrewUndoRedo
                    {
                        Undo = () => Undo(director, screwQcUndoRedo),
                        Redo = () => Redo(director, screwQcUndoRedo)
                    };
                    doc.AddCustomUndoEvent("OnUndoRedo", OnUndoRedo, screwUndoRedo);

                    RecreateScrewBarrels(director, casePreferenceData);
                }

                AddImplantScrewTrackingParameter(ScrewUtilities.GetScrewNumberWithPhaseNumber(screw, false));

                doc.Objects.UnselectAll();
                PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(director);
                doc.Views.Redraw();
                CasePreferencePanel.GetView().InvalidateUI();
                return result;
            }

            return Result.Failure;
        }

        private void Undo(CMFImplantDirector director, ScrewQcUndoRedo screwQCUndoRedo)
        {
            // Avoid invoking events when undoing since it is already handled in screwQCUndoRedo.IdsDocument.Undo()
            screwQCUndoRedo.CasePreferenceDataModel.Dispose();
            screwQCUndoRedo.CasePreferenceDataModel.ImplantDataModel = screwQCUndoRedo.OldImplantDataModel;
            screwQCUndoRedo.CasePreferenceDataModel.InvalidateEvents(director);

            PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(screwQCUndoRedo.CasePreferenceDataModel.ImplantDataModel.DotList);
            CasePreferencePanel.GetView().InvalidateUI();
            screwQCUndoRedo.IdsDocument.Undo();
        }

        private void Redo(CMFImplantDirector director, ScrewQcUndoRedo screwQCUndoRedo)
        {
            // Avoid invoking events when redoing since it is already handled in screwQCUndoRedo.IdsDocument.Redo()
            screwQCUndoRedo.CasePreferenceDataModel.Dispose();
            screwQCUndoRedo.CasePreferenceDataModel.ImplantDataModel = screwQCUndoRedo.NewImplantDataModel;
            screwQCUndoRedo.CasePreferenceDataModel.InvalidateEvents(director);

            PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(screwQCUndoRedo.CasePreferenceDataModel.ImplantDataModel.DotList);
            CasePreferencePanel.GetView().InvalidateUI();
            screwQCUndoRedo.IdsDocument.Redo();
        }
    }
}