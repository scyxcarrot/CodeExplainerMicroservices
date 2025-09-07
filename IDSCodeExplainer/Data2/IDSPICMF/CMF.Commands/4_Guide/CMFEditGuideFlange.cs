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
    [System.Runtime.InteropServices.Guid("10095F8D-7B8A-4F31-A5AB-FFAA6AA67F98")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFlangeGuidingOutline, IBB.GuideFlange)]
    public class CMFEditGuideFlange : CmfCommandBase
    {
        public override string EnglishName => "CMFEditGuideFlange";
        public static CMFEditGuideFlange TheCommand { get; private set; }

        public CMFEditGuideFlange()
        {
            TheCommand = this;
            VisualizationComponent = new CMFGuideFlangeVisualization();
        }
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Locking.UnlockGuideFlanges(doc);
            var objManager = new CMFObjectManager(director);      
            Guid selectedFlangeId;
            if (GetSelectedFlange(director, out selectedFlangeId))
            {
                doc.Objects.UnselectAll();
                ((CMFGuideFlangeVisualization)VisualizationComponent).HideFlanges(doc);
                doc.Views.Redraw();

                var flangeObj = director.Document.Objects.Find(selectedFlangeId);
                var guidePref = objManager.GetGuidePreference(flangeObj);

                var helper = new GuideFlangeObjectHelper(director);
                var flangeOutline = helper.GetFlangeCurve(flangeObj);
                var flangeHeight = helper.GetFlangeHeight(flangeObj);

                var flangeCurveInputGetter = new GuideFlangeInputsGetter(director);
                Curve editedCurve;
                double editedHeight;
                if (flangeCurveInputGetter.EditInputs(flangeOutline, flangeHeight, out editedCurve, out editedHeight) != GuideFlangeInputsGetter.EResult.Success)
                {
                    doc.Views.Redraw();
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Edit Guide Flange Outline Failed.");
                    return Result.Failure;
                }

                Mesh outputFlange;
                var createGuideFlange = new GuideFlangeCreation(director, flangeCurveInputGetter.OsteotomyParts);
                if (!createGuideFlange.GenerateGuideFlange(editedCurve, editedHeight, out outputFlange))
                {
                    return Result.Failure;
                }

                helper.ReplaceExistingFlange(guidePref, selectedFlangeId, outputFlange, editedCurve, editedHeight);

                guidePref.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.GuideFlange });

                doc.Views.Redraw();
                return Result.Success;
            }

            return Result.Success;
        }

        private bool GetSelectedFlange(CMFImplantDirector director, out Guid selectedFlangeId)
        {
            selectedFlangeId = Guid.Empty;
            director.Document.Views.Redraw();

            var getObject = new GetObject();
            getObject.SetCommandPrompt("Select Guide Flange to Edit, Enter to accept, or Esc to cancel changes");
            getObject.EnablePreSelect(false, false);
            getObject.EnablePostSelect(true);
            getObject.AcceptNothing(true);
            getObject.EnableTransparentCommands(false);

            var res = getObject.Get();
            if (res == GetResult.Object)
            {
                director.Document.Views.Redraw();
                selectedFlangeId = getObject.Object(0).ObjectId;
                return true;
            }
            return false;
        }
    }
}
