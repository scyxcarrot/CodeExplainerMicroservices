using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.CommandBase;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;

namespace IDS.Common.Commands
{
    /**
     * Rhino command to make a cup screw.
     */

    [System.Runtime.InteropServices.Guid("F439EA0A-0345-4FED-881B-BB6B98A165F0")]
    [IDSCommandAttributes(true, DesignPhase.Reaming, IBB.ExtraReamingEntity)]
    public class VersionCheck : CommandBase<ImplantDirector>
    {
        /**
         * Initialize singleton instance representing this command.
         */

        public VersionCheck()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /** The one and only instance of this command */

        public static VersionCheck TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "VersionCheck";

        /**
         * Run the command to select and delete reaming entities
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            VersionControl.DoVersionCheck(director, true, true, string.Empty, PlugInInfo.PluginModel);
            return Result.Success;
        }
    }
}