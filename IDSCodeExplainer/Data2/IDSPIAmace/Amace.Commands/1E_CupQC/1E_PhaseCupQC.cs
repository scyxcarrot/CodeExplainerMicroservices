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
     * Rhino Command to Start the cup qc phase
     */

    [System.Runtime.InteropServices.Guid("A220B815-0DF9-465A-B61C-C6C76CD298B0")]
    [IDSCommandAttributes(true, ~DesignPhase.Draft, IBB.DesignPelvis, IBB.Cup, IBB.SkirtMesh, IBB.ScaffoldVolume)]
    public class StartCupQCCommand : CommandBase<ImplantDirector>
    {
        public StartCupQCCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static StartCupQCCommand TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "StartCupQC";

        /**
        * RunCommand does .... as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Define target phase
            const DesignPhase targetPhase = DesignPhase.CupQC;
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