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
    [System.Runtime.InteropServices.Guid("86A72E9A-C42E-43E7-B67F-C1F455C2A0C7")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.Screw)]
    public class AlignScrewEnds : CommandBase<GleniusImplantDirector>
    {
        public AlignScrewEnds()
        {
            TheCommand = this;
            VisualizationComponent = new AddScrewVisualization()
            {
                EnableOnCommandSuccessVisualization = false
            };
        }

        public static AlignScrewEnds TheCommand { get; private set; }

        public override string EnglishName => "AlignScrewEnds";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {

            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            bool alignHead;
            var response = GetOption(out alignHead);
            if (response != Result.Success)
            {
                return response;
            }

            // Unlock screws
            Locking.UnlockScrews(director.Document);
            // Select screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select a screw");
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            var res = selectScrew.Get();
            if (res == GetResult.Object)
            {
                // Get selected screw
                var screw = selectScrew.Object(0).Object() as Screw;
                var operation = new AlignScrew(screw, alignHead);
                var result = operation.AdjustLength();
                doc.Objects.UnselectAll();
                doc.Views.Redraw();
                return result;
            }

            return Result.Failure;
        }

        public Result GetOption(out bool alignScrewHead)
        {
            alignScrewHead = true;

            var getOption = new GetOption();
            getOption.SetCommandPrompt("Choose to align screw head or tip");
            var alignHead = getOption.AddOption("Head");
            getOption.AddOption("Tip");
            getOption.EnableTransparentCommands(false);
            getOption.Get();

            if (getOption.CommandResult() != Result.Success)
            {
                return getOption.CommandResult();
            }

            var option = getOption.Option();
            if (option == null)
            {
                return Result.Failure;
            }
            var optionSelected = option.Index;

            alignScrewHead = optionSelected == alignHead;
            return Result.Success;
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