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
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("0CE40557-4FEC-4CC5-867F-59A8757387D9")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFixationScrew)]
    public class CMFAdjustGuideFixationScrewLength : CmfCommandBase
    {
        public CMFAdjustGuideFixationScrewLength()
        {
            TheCommand = this;
            VisualizationComponent = new CMFManipulateGuideFixationScrewVisualization();
        }
        
        public static CMFAdjustGuideFixationScrewLength TheCommand { get; private set; }
        
        public override string EnglishName => "CMFAdjustGuideFixationScrewLength";
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Locking.UnlockGuideFixationScrewsExceptShared(director);

            var selectGuideFixationScrew = new GetObject();
            selectGuideFixationScrew.SetCommandPrompt("Select a guide fixation screw to adjust it's length.");
            selectGuideFixationScrew.EnablePreSelect(false, false);
            selectGuideFixationScrew.EnablePostSelect(true);
            selectGuideFixationScrew.AcceptNothing(true);
            selectGuideFixationScrew.EnableTransparentCommands(false);

            var res = selectGuideFixationScrew.Get();
            if (res == GetResult.Object)
            {
                var screw = selectGuideFixationScrew.Object(0).Object() as Screw;
                var availableLengths = ScrewUtilities.GetAvailableScrewLengths(screw, true);
                var operation = new AdjustGuideFixationScrewLength(screw, availableLengths);
                var result = operation.AdjustLength();
                doc.Objects.UnselectAll();
                doc.Views.Redraw();

                var screwNumber = ScrewUtilities.GetScrewNumberWithPhaseNumber(screw, true);
                TrackingParameters.Add("Affected Screw", screwNumber);

                if (result == Result.Success && operation.NeedToClearUndoRedoRecords)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, "Adjust a shared screw length will clear Undo/Redo upon success, and you will no longer able undo to your previous operations.");
                    doc.ClearUndoRecords(true);
                    doc.ClearRedoRecords();
                    director.IdsDocument?.ClearUndoRedo();
                }

                return result;
            }

            return Result.Failure;
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