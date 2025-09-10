using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Common;
using IDS.Core.CommandBase;
using Rhino;
using Rhino.Commands;

namespace IDS.Amace.Commands
{
    /**
     * Rhino Command to Start the scaffold phase
     */

    [System.Runtime.InteropServices.Guid("A3B944EC-6D4C-4EDD-BCF1-A29452F23311")]
    [IDSCommandAttributes(true, ~DesignPhase.Draft, IBB.DesignPelvis, IBB.Cup, IBB.SkirtMesh, IBB.SkirtGuide)]
    public class StartScaffoldCommand : CommandBase<ImplantDirector>
    {
        public StartScaffoldCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static StartScaffoldCommand TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "StartScaffold";

        /**
        * RunCommand does .... as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Define target phase
            var targetPhase = DesignPhase.Scaffold;
            var success = PhaseChanger.ChangePhase(director, targetPhase);
            if (!success)
            {
                return Result.Failure;
            }

            // Success
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}