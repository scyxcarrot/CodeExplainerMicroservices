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
     * Rhino Command to Start the implant qc phase
     */

    [System.Runtime.InteropServices.Guid("C36EC87F-3250-4097-8488-A57773363D4B")]
    [IDSCommandAttributes(true, ~DesignPhase.Draft, IBB.DesignPelvis, IBB.Cup, IBB.SkirtMesh, IBB.ScaffoldVolume, IBB.PlateBumps)]
    public class StartImplantQCCommand : CommandBase<ImplantDirector>
    {
        public StartImplantQCCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static StartImplantQCCommand TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "StartImplantQC";

        /**
        * RunCommand does .... as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Define target phase
            const DesignPhase targetPhase = DesignPhase.ImplantQC;
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