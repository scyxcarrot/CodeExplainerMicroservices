using IDS.CMF;
using IDS.CMF.AttentionPointer;
using IDS.CMF.CasePreferences;
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
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("7931DDC6-F991-450A-BF8F-6691B29BCEC2")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.ImplantSupport, IBB.Screw)]
    public class CMFRotateMultiScrewsHead : CMFImplantScrewBaseCommand
    {
        private struct RotateScrewsUndoRedoParam
        {
            public Screw screw;
            public Screw newScrew;
            public List<IDot> dotList;
            public string prevPastilleAlgo;
        }

        public CMFRotateMultiScrewsHead()
        {
            TheCommand = this;
            VisualizationComponent = new CMFManipulateImplantScrewVisualization();
            IsUseBaseCustomUndoRedo = false;
        }

        /// The one and only instance of this command
        public static CMFRotateMultiScrewsHead TheCommand { get; private set; }

        /// The command name as it appears on the Rhino command line
        public override string EnglishName => "CMFRotateMultiScrewsHead";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            // Get selected screw
            var screws = SelectMultipleScrew(doc, "Select multiple screws to rotate it's head and press Enter");

            if (screws != null && screws.Any())
            {
                var objectManager = new CMFObjectManager(director);
                var implantSupportManager = new ImplantSupportManager(objectManager);

                var casePreferenceUsed = screws.Select(screw => objectManager.GetCasePreference(screw)).Distinct().ToList();
                var implantSupportsInvolved = new Dictionary<CasePreferenceDataModel, RhinoObject>();
                casePreferenceUsed.ForEach(casePref =>
                {
                    var implantSupportRhObj = implantSupportManager.GetImplantSupportRhObj(casePref);
                    implantSupportManager.ImplantSupportNullCheck(implantSupportRhObj, casePref);
                    implantSupportsInvolved[casePref] = implantSupportRhObj;
                });

                var batchOperation = new BatchInvertedRotateImplantScrewManager(screws, implantSupportsInvolved);

                Result result = Result.Nothing;
                var roIVisualizers = new List<ImplantSurfaceRoIVisualizer>();

                try
                {
                    foreach (var implantSupportInvolved in implantSupportsInvolved)
                    {
                        var casePreference = implantSupportInvolved.Key;
                        var implantSupportRhObj = implantSupportInvolved.Value;

                        var roIVisualizer = new ImplantSurfaceRoIVisualizer(casePreference, implantSupportRhObj)
                        {
                            Enabled = true
                        };
                        roIVisualizers.Add(roIVisualizer);
                    }

                    result = batchOperation.RotateAllScrews();
                }
                catch (Exception e)
                {
                    Msai.TrackException(e, "CMF");
                }

                foreach (var roiVisualizer in roIVisualizers)
                {
                    roiVisualizer.Enabled = false;
                    roiVisualizer?.Dispose();
                }

                if (result == Result.Success)
                {
                    var screwsUndoRedoParam = new List<RotateScrewsUndoRedoParam>();

                    foreach (var screw in screws)
                    {
                        var screwId = screw.Id;
                        var newScrew = (Screw)director.Document.Objects.Find(screwId);

                        var casePref = objectManager.GetCasePreference(screw);
                        var rotationCenterPastille =
                            ScrewUtilities.FindDotTheScrewBelongsTo(screw, casePref.ImplantDataModel.DotList);

                        var prevAlgo = rotationCenterPastille.CreationAlgoMethod;
                        ImplantPastilleCreationUtilities.UpdatePastilleAlgo(casePref.ImplantDataModel.DotList, screw.Id, DotPastille.CreationAlgoMethods[0]);
                        casePref.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.Screw }, IBB.Landmark, IBB.Connection, IBB.RegisteredBarrel, 
                            IBB.PastillePreview, IBB.ConnectionPreview);

                        var param = new RotateScrewsUndoRedoParam()
                        {
                            screw = screw,
                            newScrew = newScrew,
                            dotList = casePref.ImplantDataModel.DotList,
                            prevPastilleAlgo = prevAlgo
                        };

                        screwsUndoRedoParam.Add(param);
                    }

                    var screwUndoRedo = new ScrewUndoRedo
                    {
                        Undo = () => Undo(screwsUndoRedoParam, director.IdsDocument),
                        Redo = () => Redo(screwsUndoRedoParam, director.IdsDocument)
                    };
                    doc.AddCustomUndoEvent("OnUndoRedo", OnUndoRedo, screwUndoRedo);

                    foreach (var casePreference in casePreferenceUsed)
                    {
                        RecreateScrewBarrels(director, casePreference);
                    }
                }

                doc.Objects.UnselectAll();
                PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(director);
                doc.Views.Redraw();
                CasePreferencePanel.GetView().InvalidateUI();
                return result;
            }

            return Result.Failure;
        }

        private static void Undo(IEnumerable<RotateScrewsUndoRedoParam> screwsUndoRedoParam, IDSDocument document)
        {
            foreach (var param in screwsUndoRedoParam)
            {
                ImplantPastilleCreationUtilities.UpdatePastilleAlgo(param.dotList, param.screw.Id, param.prevPastilleAlgo);
                PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(param.dotList);
            }
            CasePreferencePanel.GetView().InvalidateUI();
            document.Undo();
        }

        private static void Redo(IEnumerable<RotateScrewsUndoRedoParam> screwsUndoRedoParam, IDSDocument document)
        {
            foreach (var param in screwsUndoRedoParam)
            {
                ImplantPastilleCreationUtilities.UpdatePastilleAlgo(param.dotList, param.newScrew.Id, DotPastille.CreationAlgoMethods[0]);
                PastilleAttentionPointer.Instance.RefreshHighlightedPastillePosition(param.dotList);
            }
            CasePreferencePanel.GetView().InvalidateUI();
            document.Redo();
        }
    }
}