using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("CBE3656C-254A-4D3C-9317-A2414768784A")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFlange)]
    public class CMFDeleteGuideFlange : CmfCommandBase
    {
        public static CMFDeleteGuideFlange TheCommand { get; private set; }

        public override string EnglishName => "CMFDeleteGuideFlange";
        public CMFDeleteGuideFlange()
        {
            TheCommand = this;
            VisualizationComponent = new CMFGuideFlangeVisualization();
        }

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Locking.UnlockGuideFlanges(director.Document);

            var selectGuideFlange = new GetObject();
            selectGuideFlange.SetCommandPrompt("Select guide flange(s) to delete.");
            selectGuideFlange.EnablePreSelect(false, false);
            selectGuideFlange.EnablePostSelect(true);
            selectGuideFlange.AcceptNothing(true);
            selectGuideFlange.EnableTransparentCommands(false);
            var result = Result.Failure;
            var res = selectGuideFlange.GetMultiple(0, 0);
            if (res == GetResult.Cancel)
            {
                return Result.Cancel;
            }

            if (res == GetResult.Object || res == GetResult.Nothing)
            {
                var selectedGuideFlanges = doc.Objects.GetSelectedObjects(false, false).ToList();
                var removed = DeleteGuideFlanges(director, selectedGuideFlanges);
                result = removed ? Result.Success : Result.Failure;
            }
            return result;
        }

        private bool DeleteGuideFlanges(CMFImplantDirector director, List<RhinoObject> rhinoObjects)
        {
            var objectManager = new CMFObjectManager(director);

            foreach (var rhobj in rhinoObjects)
            {
                objectManager.DeleteObject(rhobj.Id);

                var guidePref = objectManager.GetGuidePreference(rhobj);
                guidePref.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.GuideFlange });
            }
            return true;
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
