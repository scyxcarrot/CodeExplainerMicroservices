using IDS.Core.PluginHelper;
using IDS.Glenius;
using Rhino;
using Rhino.Commands;

namespace IDS.Common.Commands
{
    /**
     * Rhino command to make a cup screw.
     */

    [System.Runtime.InteropServices.Guid("F439EA0A-0345-4FED-881B-BB6B98A165F0")]
    public class VersionCheck : Rhino.Commands.Command
    {
        /**
         * Initialize singleton instance representing this command.
         */

        public VersionCheck()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        /** The one and only instance of this command */

        public static VersionCheck TheCommand => m_thecommand;

        private static VersionCheck m_thecommand;

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "VersionCheck";

        /**
         * Run the command to select and delete reaming entities
         */

        protected override Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector(doc.DocumentId);
            VersionControl.DoVersionCheck(director, true, true, string.Empty, PlugInInfo.PluginModel);

            // Success
            return CommandSucceeded(doc);
        }

        // This is called everytime the command fails
        private Result CommandFailed(RhinoDoc doc)
        {
            // Exiting command
            return Result.Failure;
        }

        // This is called everytime the command succeeds
        private Result CommandSucceeded(RhinoDoc doc)
        {
            // Exiting command
            return Result.Success;
        }
    }
}