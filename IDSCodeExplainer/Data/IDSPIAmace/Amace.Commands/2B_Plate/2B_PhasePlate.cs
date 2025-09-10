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
     * Rhino Command to Start the plate phase
     */

    [System.Runtime.InteropServices.Guid("29B50B84-C1D6-484A-91D5-122484395932")]
    [IDSCommandAttributes(true, ~DesignPhase.Draft, IBB.DesignPelvis, IBB.Cup, IBB.SkirtMesh, IBB.ScaffoldVolume)]
    public class StartPlateCommand : CommandBase<ImplantDirector>
    {
        public StartPlateCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static StartPlateCommand TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "StartPlate";

        /**
        * RunCommand does .... as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {

            // Define target phase
            const DesignPhase targetPhase = DesignPhase.Plate;
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