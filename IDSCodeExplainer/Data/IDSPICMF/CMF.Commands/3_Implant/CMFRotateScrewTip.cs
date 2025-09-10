using IDS.CMF;
using IDS.CMF.AttentionPointer;
using IDS.CMF.CommandHelpers;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.PluginHelper;
using IDS.Core.V2.TreeDb.Model;
using IDS.Interface.Implant;
using IDS.PICMF.Forms;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("BCA9CF2D-9D1F-4CE7-91BC-A846986A726C")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.ImplantSupport, IBB.Screw)]
    public class CMFRotateScrewTip : CMFImplantScrewBaseCommand
    {
        public CMFRotateScrewTip()
        {
            TheCommand = this;
            VisualizationComponent = new CMFManipulateImplantScrewVisualization();
            IsUseBaseCustomUndoRedo = false;
        }

        /// The one and only instance of this command
        public static CMFRotateScrewTip TheCommand { get; private set; }

        /// The command name as it appears on the Rhino command line
        public override string EnglishName => "CMFRotateScrewTip";
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            // Get selected screw
            var screw = SelectScrew(doc, "Select a screw to rotate it's tip.");

            if (screw != null)
            {
                screw.Highlight(false);
                var objectManager = new CMFObjectManager(director);
                var casePreference = objectManager.GetCasePreference(screw);

                var rotationCenterPastille =
                    ScrewUtilities.FindDotTheScrewBelongsTo(screw, casePreference.ImplantDataModel.DotList);

                var operation = new RotateImplantScrew(screw, RhinoPoint3dConverter.ToPoint3d(rotationCenterPastille.Location), 
                    -RhinoVector3dConverter.ToVector3d(rotationCenterPastille.Direction));

                var implantSupportManager = new ImplantSupportManager(objectManager);
                var rhSupport = implantSupportManager.GetImplantSupportRhObj(casePreference);
                implantSupportManager.ImplantSupportNullCheck(rhSupport, casePreference);

                operation.OldImplantDataModel = casePreference.ImplantDataModel.Clone() as ImplantDataModel;

                operation.ConstraintMesh =
                    ImplantCreationUtilities.GetImplantRoIVolumeWithoutCheck(objectManager, casePreference, ref rhSupport);

                ImplantSurfaceRoIVisualizer RoIVisualizer = null;
                Result result = Result.Nothing;

                try
                {
                    RoIVisualizer = new ImplantSurfaceRoIVisualizer(casePreference, rhSupport);
                    RoIVisualizer.Enabled = true;
                    result = operation.Rotate(true);
                }
                catch (Exception e)
                {
                    Msai.TrackException(e, "CMF");
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

                    var casePref = objectManager.GetCasePreference(screw);
                    var prevAlgo = rotationCenterPastille.CreationAlgoMethod;
                    ImplantPastilleCreationUtilities.UpdatePastilleAlgo(casePreference.ImplantDataModel.DotList, screw.Id, DotPastille.CreationAlgoMethods[0]);
                    casePref.Graph.NotifyBuildingBlockHasChanged(new[] {IBB.Screw}, IBB.Landmark, IBB.Connection, IBB.RegisteredBarrel, IBB.PastillePreview, IBB.ConnectionPreview);
                    
                    var screwUndoRedo = new ScrewUndoRedo
                    {
                        Undo = () => Undo(screw, casePreference.ImplantDataModel.DotList, prevAlgo, director.IdsDocument),
                        Redo = () => Redo(newScrew, casePreference.ImplantDataModel.DotList, DotPastille.CreationAlgoMethods[0], director.IdsDocument)
                    };
                    doc.AddCustomUndoEvent("OnUndoRedo", OnUndoRedo, screwUndoRedo);

                    RecreateScrewBarrels(director, casePreference);
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

        private void Undo(Screw screw, List<IDot> dotList, string algo, IDSDocument document)
        {
            ImplantPastilleCreationUtilities.UpdatePastilleAlgo(dotList, screw.Id, algo);
            PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(dotList);
            CasePreferencePanel.GetView().InvalidateUI();
            document.Undo();
        }

        private void Redo(Screw screw, List<IDot> dotList, string algo, IDSDocument document)
        {
            ImplantPastilleCreationUtilities.UpdatePastilleAlgo(dotList, screw.Id, algo);
            PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(dotList);
            CasePreferencePanel.GetView().InvalidateUI();
            document.Redo();
        }

    }
}