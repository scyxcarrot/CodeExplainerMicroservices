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
    [System.Runtime.InteropServices.Guid("9CF1B37C-AB42-45DF-AA45-F94E9A65FD67")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideBridge)]
    public class CMFDeleteGuideBridge : CmfCommandBase
    {
        public CMFDeleteGuideBridge()
        {
            TheCommand = this;
            VisualizationComponent = new CMFGuideBridgeVisualization();
        }

        public static CMFDeleteGuideBridge TheCommand { get; private set; }
        
        public override string EnglishName => "CMFDeleteGuideBridge";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Locking.UnlockGuideBridge(director.Document);

            var selectGuideBridge = new GetObject();
            selectGuideBridge.SetCommandPrompt("Select guide bridge(s) to delete.");
            selectGuideBridge.EnablePreSelect(false, false);
            selectGuideBridge.EnablePostSelect(true);
            selectGuideBridge.AcceptNothing(true);
            selectGuideBridge.EnableTransparentCommands(false);

            var result = Result.Failure;

            while (true)
            {
                var res = selectGuideBridge.GetMultiple(0, 0);

                if (res == GetResult.Cancel)
                {
                    break;
                }

                if (res == GetResult.Object || res == GetResult.Nothing)
                {                    
                var selectedGuideBridges = doc.Objects.GetSelectedObjects(false, false).ToList();
                var removed = DeleteGuideBridges(director, selectedGuideBridges);
                result = removed ? Result.Success : Result.Failure;

                // Stop user input
                break;  
                }
            }

            return result;
        }

        private bool DeleteGuideBridges(CMFImplantDirector director, List<RhinoObject> rhinoObjects)
        {
            var objectManager = new CMFObjectManager(director);

            foreach (var rhobj in rhinoObjects)
            {
                objectManager.DeleteObject(rhobj.Id);
                var casePreferenceData = objectManager.GetGuidePreference(rhobj);
                casePreferenceData.Graph.NotifyBuildingBlockHasChanged(new[] {IBB.GuideBridge});

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