using IDS.Amace.Enumerators;
using IDS.Amace.GUI;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Proxies;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Amace.Commands
{
    /**
     * Rhino command to make a cup screw.
     */

    [System.Runtime.InteropServices.Guid("D4107DD1-BE4B-43AD-834E-96C3C070F9C0")]
    [IDSCommandAttributes(true, DesignPhase.Screws, IBB.Cup, IBB.WrapBottom, IBB.WrapTop, IBB.WrapSunkScrew)]
    public class DeleteScrews : CommandBase<ImplantDirector>
    {
        /**
         * Initialize singleton instance representing this command.
         */

        public DeleteScrews()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        /** The one and only instance of this command */

        public static DeleteScrews TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "DeleteScrews";

        private readonly Dependencies _dependencies;

        /**
         * Run the command to make a screw.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Unlock screws
            Locking.UnlockScrews(director.Document);
            // Get screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select screws to remove.");
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            // Get user input
            while (true)
            {
                var res = selectScrew.GetMultiple(0, 0);

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    return Result.Failure;
                }

                if (res == GetResult.Object)
                {
                    // Ask confirmation and delete if user clicks 'Yes'
                    var result = Rhino.UI.Dialogs.ShowMessageBox(
                        "Are you sure you want to delete the selected screw(s)?",
                        "Delete Screw(s)?",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Exclamation);
                    if (result == DialogResult.Yes)
                    {
                        // Get selected objects
                        var selectedScrews = doc.Objects.GetSelectedObjects(false, false).ToList();
                        // Delete one by one (including dependencies)
                        foreach (var rhobj in selectedScrews)
                        {
                            var screw = (Screw)rhobj;
                            screw.Delete();
                        }

                        // Stop user input
                        break;
                    }
                    if (result == DialogResult.Cancel)
                    {
                        return Result.Failure;
                    }
                }
            }

            // Update screw checks
            ScrewInfo.Update(doc, false);

            // Refresh panel
            var screwPanel = ScrewPanel.GetPanel();
            screwPanel?.RefreshPanelInfo();

            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            _dependencies.DeleteBlockDependencies(director, IBB.Screw);
            Visibility.ScrewDefault(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            _dependencies.DeleteBlockDependencies(director, IBB.Screw); 
            Visibility.ScrewDefault(doc);
        }
    }
}