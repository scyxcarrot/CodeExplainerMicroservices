using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
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
    [System.Runtime.InteropServices.Guid("B9CA3DA4-0DE7-4600-915E-1AF7AE12B839")]
    [IDSCMFCommandAttributes(DesignPhase.Guide | DesignPhase.TeethBlock)]
    public class CMFDeleteTeethBlock : CmfCommandBase
    {
        public CMFDeleteTeethBlock()
        {
            TheCommand = this;
            VisualizationComponent = new CMFDeleteTeethBlockVisualization();
        }

        public static CMFDeleteTeethBlock TheCommand { get; private set; }

        public override string EnglishName => "CMFDeleteTeethBlock";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var allGuidePreferences = director.CasePrefManager.GuidePreferences;
            var teethBlockEibbs = objectManager.GetAllGuideExtendedImplantBuildingBlocks(IBB.TeethBlock, allGuidePreferences);

            var totalTeethBlockObject = 0;
            foreach (var teethBlockEibb in teethBlockEibbs)
            {
                var teethBlocks = objectManager
                    .GetAllBuildingBlocks(teethBlockEibb);
                totalTeethBlockObject += teethBlocks.Count();
            }

            if (totalTeethBlockObject == 0)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "There is no teeth block available");
                return Result.Cancel;
            }

            Locking.UnlockTeethBlock(director.Document);

            var selectTeethBlocks = new GetObject();
            selectTeethBlocks.SetCommandPrompt("Select teeth block to delete.");
            selectTeethBlocks.EnablePreSelect(false, false);
            selectTeethBlocks.EnablePostSelect(true);
            selectTeethBlocks.AcceptNothing(true);
            selectTeethBlocks.EnableTransparentCommands(false);

            while (true)
            {
                var res = selectTeethBlocks.GetMultiple(0, 0);

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    break;
                }

                if (res == GetResult.Object)
                {
                    var selectedEntities = doc.Objects.GetSelectedObjects(false, false).ToList();

                    DeleteSelectedEntities(director, selectedEntities);
                    foreach (var selectedEntity in selectedEntities)
                    {
                        var guidePrefModel = objectManager
                            .GetGuidePreference(selectedEntity);
                        guidePrefModel.Graph.NotifyBuildingBlockHasChanged(
                            new[] { IBB.TeethBlock });
                    }
                    // Stop user input
                    break;
                }
            }
            return Result.Success;
        }

        private void DeleteSelectedEntities(CMFImplantDirector director, List<RhinoObject> rhinoObjects)
        {
            var objectManager = new CMFObjectManager(director);
            foreach (var rhinoObject in rhinoObjects)
            {
                objectManager.DeleteObject(rhinoObject.Id);
            }
        }
    }
}
