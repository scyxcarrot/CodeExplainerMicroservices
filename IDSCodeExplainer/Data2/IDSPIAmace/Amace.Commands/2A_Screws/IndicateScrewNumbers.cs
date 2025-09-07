using IDS.Amace.Enumerators;
using IDS.Amace.GUI;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Proxies;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.DataTypes;
using IDS.Core.PluginHelper;
using IDS.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Linq;

namespace IDS.Amace.Commands
{
    /**
     * Rhino command to inspect a screw using building blocks
     * and a clipping plane
     */

    [System.Runtime.InteropServices.Guid("158F20E5-CEFC-42F7-B250-8DF8AE8F3FDF")]
    [IDSCommandAttributes(false, DesignPhase.Screws | DesignPhase.ImplantQC, IBB.Screw)]
    public class IndicateScrewNumbers : TransformCommand
    {
        /**
         * Initialize singleton instance representing this command.
         */

        public IndicateScrewNumbers()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /** The one and only instance of this command */

        public static IndicateScrewNumbers TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "IndicateScrewNumbers";

        /** Filter callback for selecting screws */

        public bool ScrewGeometryFilter(RhinoObject rhObject)
        {
            return rhObject is Screw;
        }

        /**
         * Run the command to adjust the screw.
         */

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Check input data
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            // Unlock screws
            Locking.UnlockScrews(director.Document);
            // Get screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Click the screws one by one to assign screw numbers.");
            selectScrew.DisablePreSelect();
            selectScrew.AcceptNothing(true);
            selectScrew.EnableHighlight(false);
            // Clipping plane ID
            var newIndex = 1;
            var screwManager = new ScrewManager(director.Document);
            var screws = screwManager.GetAllScrews().ToList();
            foreach (var sc in screws)
            {
                sc.Index = -1;
            }

            // Hide QC conduit
            ScrewInfo.Disable(doc);
            // Replace the conduit by a new one, to make sure the information is up to date
            var numbers = new ScrewConduit(director, ScrewConduitMode.NoWarnings)
            {
                Enabled = true
            };
            // Set perspective view to parallel projection
            Visibility.ScrewNumbers(doc);

            // Get user input
            while (newIndex <= screws.Count)
            {
                var res = selectScrew.Get(); // redraws before and after getting
                switch (res)
                {
                    case GetResult.Cancel:
                        {
                            // Reset
                            foreach (Screw sc in screws)
                            {
                                sc.Index = -1;
                            }
                            // Refresh
                            newIndex = 1;
                            break;
                        }
                    case GetResult.Object:
                        {
                            // Also called when object was preselected
                            var screw = (Screw)selectScrew.Object(0).Object();
                            if (screw.Index == -1)
                            {
                                screw.Index = newIndex;
                                newIndex++;
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
            numbers.Enabled = false;

            // Update screw checks
            ScrewInfo.Update(doc, false);

            var screwPanel = ScrewPanel.GetPanel();
            screwPanel?.RefreshPanelInfo();

            return CommandSucceeded(doc);
        }

        public static Result CommandFailed(RhinoDoc doc)
        {
            Visibility.ScrewDefault(doc);
            return Result.Failure;
        }

        public static Result CommandSucceeded(RhinoDoc doc)
        {
            Visibility.ScrewDefault(doc);
            return Result.Success;
        }
    }
}