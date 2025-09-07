using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Diagnostics;

namespace IDSPIGlenius.Commands
{
    [System.Runtime.InteropServices.Guid("20a432f2-3e66-46aa-a6e0-d19c63c6adaa")]
    public class GleniusHelp : Command
    {
        public GleniusHelp()
        {
            Instance = this;
        }

        ///<summary>The only instance of the GleniusHelp command.</summary>
        public static GleniusHelp Instance { get; private set; }

        public override string EnglishName => "GleniusHelp";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            const string idsMainHelp = "toolbarprojecttabIDSMain";
            const string toolbarProject = "toolbarproject";
            const string toolbarReconstruction = "toolbarreconstruction";
            const string toolbarHead = "toolbarhead";
            const string toolbarScrews = "toolbarscrews";
            const string toolbarScrewQc = "toolbarscrewqc";
            const string toolbarPlate = "toolbarplate";
            const string toolbarScaffold = "toolbarscaffold";
            const string toolbarScaffoldQc = "toolbarscaffoldqc";

            var go = new GetOption();
            go.SetCommandPrompt("Choose which toolbar you want help for.");
            go.AcceptNothing(true);
            go.AcceptString(true);
            // Placement method
            var toolbars = new List<string> { idsMainHelp, toolbarProject, toolbarReconstruction, toolbarHead,
                toolbarScrews, toolbarScrewQc, toolbarPlate, toolbarScaffold, toolbarScaffoldQc };
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

                if (res == GetResult.String)
                {
                    selectedHelp = go.StringResult();
                    break;
                }
            }

            var rc = go.CommandResult();
            if (rc != Result.Success)
            {
                return Result.Failure;
            }

            var resources = new IDS.Glenius.Resources();
            string helpDocumentUrl; //if String is empty or idsMainHelp

            switch(selectedHelp)
            {
                case toolbarProject:
                    helpDocumentUrl = resources.GleniusToolbarProjectUrl;
                    break;
                case toolbarReconstruction:
                    helpDocumentUrl = resources.GleniusToolbarReconstructionUrl;
                    break;
                case toolbarHead:
                    helpDocumentUrl = resources.GleniusToolbarHeadUrl;
                    break;
                case toolbarScrews:
                    helpDocumentUrl = resources.GleniusToolbarScrewUrl;
                    break;
                case toolbarScrewQc:
                    helpDocumentUrl = resources.GleniusToolbarScrewQcUrl;
                    break;
                case toolbarPlate:
                    helpDocumentUrl = resources.GleniusToolbarPlateUrl;
                    break;
                case toolbarScaffold:
                    helpDocumentUrl = resources.GleniusToolbarScaffoldUrl;
                    break;
                case toolbarScaffoldQc:
                    helpDocumentUrl = resources.GleniusToolbarScaffoldQcUrl;
                    break;
                default:
                    helpDocumentUrl = resources.GleniusGeneralInfoUrl;
                    break;
            }

            Process.Start(SystemTools.GetSystemDefaultBrowser(), helpDocumentUrl);

            return Result.Success;
        }
    }
}
