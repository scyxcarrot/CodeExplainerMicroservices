using IDS.Amace.Enumerators;
using IDS.Core.CommandBase;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Diagnostics;

namespace IDS.Common.Commands
{
    /**
     * Rhino command to make a cup screw.
     */

    [System.Runtime.InteropServices.Guid("76041C54-6133-454A-B58C-C4017706C371")]
    [IDSCommandAttributes(true, DesignPhase.Any)]
    public class IdsHelp : CommandBase<ImplantDirector>
    {
        /**
         * Initialize singleton instance representing this command.
         */

        public IdsHelp()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /** The one and only instance of this command */

        public static IdsHelp TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "IDSHelp";

        /**
         * Run the command to make a screw.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Check, can't depend on director here.
            if (!IDSPIAmacePlugIn.CheckIfCommandIsAllowed(this))
            {
                return Result.Failure;
            }

            // Ask screw options
            var go = new GetOption();
            go.SetCommandPrompt("Choose which toolbar you want help for.");
            go.AcceptNothing(true);
            go.AcceptString(true);
            // Placement method
            var toolbars = new List<string> { "toolbarproject", "toolbarcup",
                "toolbarreaming", "toolbarskirt", "toolbarscaffold", "toolbarcupqc",
                "toolbarscrews", "toolbarplate", "toolbarimplantqc" };

            var options = new List<string>();
            options.AddRange(toolbars);
            go.AddOptionList("Topic", options, 0);
            var selectedHelp = "";

            // Get user input
            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    return Result.Failure;
                }

                if (res == GetResult.Nothing)
                {
                    break;
                }

                if (res == GetResult.Option)
                {
                    selectedHelp = options[go.Option().CurrentListOptionIndex];
                    break;
                }

                if (res != GetResult.String)
                {
                    continue;
                }

                selectedHelp = go.StringResult();
                break;
            }

            var rc = go.CommandResult();
            if (rc != Result.Success)
            {
                return Result.Failure;
            }

            // Open required help file
            var resources = new Resources();
            if (selectedHelp == string.Empty)
            {
                Process.Start(resources.IdsHelpGeneralInfo);
            }
            else if (toolbars.Contains(selectedHelp))
            {
                Process.Start(SystemTools.GetSystemDefaultBrowser(), resources.GetToolbarHelpUrl(selectedHelp.Substring(7)));
            }

            return Result.Success;
        }
    }
}