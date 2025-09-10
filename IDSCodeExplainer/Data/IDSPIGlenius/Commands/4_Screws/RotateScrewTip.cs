using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("6A1894AE-5249-49A1-AC1D-6D058B244E2B")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.Screw)]
    public class RotateScrewTip : CommandBase<GleniusImplantDirector>
    {
        private static RotateScrewTip m_thecommand;

        public RotateScrewTip()
        {
            m_thecommand = this;
            VisualizationComponent = new AddScrewVisualization()
            {
                EnableOnCommandSuccessVisualization = false
            };
        }

        public static RotateScrewTip TheCommand => m_thecommand;

        public override string EnglishName => "RotateScrewTip";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            // Unlock screws
            Locking.UnlockScrews(director.Document);
            // Select screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select a screw to rotate it's tip.");
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            var res = selectScrew.Get();
            if (res == GetResult.Object)
            {
                // Get selected screw
                var screw = selectScrew.Object(0).Object() as Screw;
                var operation = new RotateScrew(screw, false);
                var result = operation.Rotate();
                doc.Objects.UnselectAll();
                doc.Views.Redraw();
                return result;
            }

            return Result.Failure;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, GleniusImplantDirector director)
        {
            GlobalScrewIndexVisualizer.Initialize(director);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, GleniusImplantDirector director)
        {
            GlobalScrewIndexVisualizer.Initialize(director);
        }
    }
}