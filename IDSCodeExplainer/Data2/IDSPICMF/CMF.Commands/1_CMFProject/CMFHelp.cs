using IDS.Core.Enumerators;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("E43384B5-2513-470F-BF68-1485301D5080")]
    public class CMFHelp : Command
    {
        public CMFHelp()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /** The one and only instance of this command */

        public static CMFHelp TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "CMFHelp";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var go = new GetOption();
            go.SetCommandPrompt("Choose which toolbar you want help for.");
            go.AcceptNothing(true);
            go.AcceptString(true);
            // Placement method
            var toolbars = new List<string> { "toolbarproject", "toolbarplanning" };

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
            IDSPICMFPlugIn.WriteLine(LogCategory.Default, $"Opening {selectedHelp}...");

            return Result.Success;
        }
    }
}