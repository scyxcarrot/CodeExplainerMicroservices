using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Glenius.CommandHelpers;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("0e2762a1-e340-4134-8e3b-699425fd490b")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Draft, IBB.ScaffoldBottom)]
    public class GleniusQcApprovedExport : CommandBase<GleniusImplantDirector>
    {
        public GleniusQcApprovedExport()
        {
            Instance = this;
        }

        ///<summary>The only instance of the GleniusQCApprovedExport command.</summary>
        public static GleniusQcApprovedExport Instance { get; private set; }

        public override string EnglishName => "GleniusQCApprovedExport";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var getOption = new GetOption();
            getOption.SetCommandPrompt("Type");
            getOption.AcceptNothing(true);
            var productionRodChamfer = new OptionToggle(false, "False", "True");
            getOption.AddOptionToggle("ProductionRodChamfer", ref productionRodChamfer);
            getOption.EnableTransparentCommands(false);
            var result = getOption.Get();

            if (result == GetResult.Cancel)
            {
                return Result.Failure;
            }

            var useProductionRodWithChamfer = productionRodChamfer.CurrentValue;

            var helper = new QCExportCommandHelper();
            return helper.DoQCExport(director, DocumentType.ApprovedQC, useProductionRodWithChamfer) ? Result.Success : Result.Failure;
        }
    }
}
