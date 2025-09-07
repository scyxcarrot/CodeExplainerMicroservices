using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("38B1CA12-5BFA-49DD-8D83-EF23A3581C2C")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFixationScrew)]
    public class CMFCreateGuideLabelTag : CmfCommandBase
    {
        public CMFCreateGuideLabelTag()
        {
            TheCommand = this;
            VisualizationComponent = new CMFGuideLabelTagVisualization();
        }
        
        public static CMFCreateGuideLabelTag TheCommand { get; private set; }
        
        public override string EnglishName => "CMFCreateGuideLabelTag";
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            // Unlock screws
            Locking.UnlockGuideFixationScrewsExceptShared(director);

            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select a guide fixation screw to attach a label tag.");
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            var res = selectScrew.Get();
            if (res == GetResult.Object)
            {
                var screw = selectScrew.Object(0).Object() as Screw;
                var sharedScrews = screw.GetScrewItSharedWith();

                Transform rotationTransform = Transform.Unset;
                double labelTagAngle = Double.NaN;
                var success = AddLabelTag(screw, director, doc, ref rotationTransform, ref labelTagAngle); // will ask user to set rotation & angle if it is unset/NaN

                sharedScrews.ForEach(s =>
                {
                    success &= AddLabelTag(s, director, doc, ref rotationTransform, ref labelTagAngle);
                });

                doc.Objects.UnselectAll();
                doc.Views.Redraw();
                return success ? Result.Success : Result.Failure;
            }

            return Result.Failure;
        }

        private bool AddLabelTag(Screw screw, CMFImplantDirector director, RhinoDoc doc, ref Transform presetRotation, ref double labelTagAngle)
        {
            var objManager = new CMFObjectManager(director);
            var hasLabelTag = screw.ScrewGuideAidesInDocument.ContainsKey(IBB.GuideFixationScrewLabelTag);
            bool success = true;

            if (hasLabelTag)
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Screw eye already have an existing label tag!");
            }
            else
            {
                Result result = Result.Failure;
                if (presetRotation == Transform.Unset)
                {
                    var operation = new CreateLabelTag(screw);
                    result = operation.Create();
                    labelTagAngle = operation.LabelTagAngle;
                }

                if (result == Result.Success || presetRotation != Transform.Unset)
                {
                    var rhinoObj = objManager.GetBuildingBlock(IBB.GuideSurfaceWrap);

                    Mesh lowLoDConstraintMesh;
                    objManager.GetBuildingBlockLoDLow(rhinoObj.Id, out lowLoDConstraintMesh);

                    var screwLabelTagHelper = new ScrewLabelTagHelper(director);

                    if (presetRotation != Transform.Unset)
                    {
                        screwLabelTagHelper.LabelTagRotation = presetRotation;
                    }

                    screwLabelTagHelper.DoAddLabelTagToScrew(screw, labelTagAngle, lowLoDConstraintMesh);

                    if (presetRotation == Transform.Unset)
                    {
                        presetRotation = screwLabelTagHelper.LabelTagRotation;
                    }

                    var casePref = objManager.GetGuidePreference(screw);
                    casePref.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.GuideFixationScrew, IBB.GuideFixationScrewLabelTag });

                    doc.ClearUndoRecords(true);
                    doc.ClearRedoRecords();
                }
                else
                {
                    success = false;
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Guide fixation screw failed to add screw label tag, please adjust/reorient the screw.");
                }
            }

            return success;
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
    }
}