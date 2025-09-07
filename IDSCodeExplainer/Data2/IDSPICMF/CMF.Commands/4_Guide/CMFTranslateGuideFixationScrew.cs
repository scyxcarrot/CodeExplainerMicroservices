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

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("41a2125b-1922-4f48-a920-ac14ddc0bad1")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFixationScrew)]
    public class CMFTranslateGuideFixationScrew : CmfCommandBase
    {
        static CMFTranslateGuideFixationScrew _instance;
        public CMFTranslateGuideFixationScrew()
        {
            _instance = this;
            VisualizationComponent = new CMFManipulateGuideFixationScrewVisualization();
        }

        ///<summary>The only instance of the CMFTranslateGuideFixationScrew command.</summary>
        public static CMFTranslateGuideFixationScrew Instance => _instance;

        public override string EnglishName => "CMFTranslateGuideFixationScrew";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            // Unlock screws
            Locking.UnlockGuideFixationScrewsExceptShared(director);

            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select a screw to translate.");
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            var res = selectScrew.Get();
            if (res == GetResult.Object)
            {
                var objectManager = new CMFObjectManager(director);

                var rhinoObj = objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap);

                Mesh lowLoDSupportMesh;
                objectManager.GetBuildingBlockLoDLow(rhinoObj.Id, out lowLoDSupportMesh);

                // Get selected screw
                var screw = (Screw)selectScrew.Object(0).Object();
                var operation = new TranslateGuideFixationScrew(screw)
                {
                    LowLoDSupportMesh = lowLoDSupportMesh
                };

                var result = operation.Translate();
                director.Document.Objects.UnselectAll();
                director.Document.Views.Redraw();

                var screwNumber = ScrewUtilities.GetScrewNumberWithPhaseNumber(screw, true);
                TrackingParameters.Add("Affected Screw", screwNumber);

                if (result == Result.Success && operation.NeedToClearUndoRedoRecords)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, "Translated a shared screw will clear Undo/Redo upon success, and you will no longer able undo to your previous operations.");
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
