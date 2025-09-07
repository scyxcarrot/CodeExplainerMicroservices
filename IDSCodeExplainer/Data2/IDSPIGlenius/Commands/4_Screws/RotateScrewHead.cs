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
    [System.Runtime.InteropServices.Guid("73A1A293-0CEB-44C0-833A-32E24998AA00")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.Screw)]
    public class RotateScrewHead : CommandBase<GleniusImplantDirector>
    {
        private static RotateScrewHead m_thecommand;

        public RotateScrewHead()
        {
            m_thecommand = this;
            VisualizationComponent = new AddScrewVisualization()
            {
                EnableOnCommandSuccessVisualization = false
            };
        }

        public static RotateScrewHead TheCommand => m_thecommand;

        public override string EnglishName => "RotateScrewHead";

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
            selectScrew.SetCommandPrompt("Select a screw to rotate it's head.");
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            var res = selectScrew.Get();
            if (res == GetResult.Object)
            {
                // Get selected screw
                var screw = (Screw)selectScrew.Object(0).Object();
                var operation = new RotateScrew(screw, true);
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