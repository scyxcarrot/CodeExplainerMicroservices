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
     * Rhino Command to Start the development case phase
     */

    [System.Runtime.InteropServices.Guid("5C51B92A-695B-4ED3-AC3C-81384C0ED353")]
    [IDSCommandAttributes(true, ~DesignPhase.Draft, IBB.DesignPelvis)]
    public class StartDevelopmentCaseCommand : CommandBase<ImplantDirector>
    {
        public StartDevelopmentCaseCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static StartDevelopmentCaseCommand TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "StartDevelopmentCase";

        /**
        * RunCommand does .... as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {

            // Define target phase
            const DesignPhase targetPhase = DesignPhase.Export;
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