using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
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

    [System.Runtime.InteropServices.Guid("FDC3F024-7424-4075-B06F-40A472912E9B")]
    [IDSCommandAttributes(true, DesignPhase.Reaming, IBB.ExtraReamingEntity)]
    public class DeleteReamingEntity : CommandBase<ImplantDirector>
    {
        /**
         * Initialize singleton instance representing this command.
         */

        public DeleteReamingEntity()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        /** The one and only instance of this command */

        public static DeleteReamingEntity TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "DeleteReamingEntity";

        private readonly Dependencies _dependencies;

        /**
         * Run the command to select and delete reaming entities
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Visualization
            Visibility.ReamingDefaultWithoutCupRbv(doc);

            // Unlock reaming blocks
            Locking.UnlockReamingEntities(director.Document);

            // Get reaming block
            var selectReamingBlocks = new GetObject();
            selectReamingBlocks.SetCommandPrompt("Select entities to remove.");
            selectReamingBlocks.EnablePreSelect(false, false);
            selectReamingBlocks.EnablePostSelect(true);
            selectReamingBlocks.AcceptNothing(true);
            // Get user input
            while (true)
            {
                var res = selectReamingBlocks.GetMultiple(0, 0);

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    return Result.Failure;
                }


                if (res != GetResult.Object)
                {
                    continue;
                }

                // Ask confirmation and delete if user clicks 'Yes'
                var result = Rhino.UI.Dialogs.ShowMessageBox(
                    "Are you sure you want to delete the selected reaming entity / entities?",
                    "Delete Reaming Entities(s)?",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Exclamation);
                if (result == DialogResult.Yes)
                {
                    // Get selected objects
                    var selectedReamingBlocks = doc.Objects.GetSelectedObjects(false, false).ToList();
                    // Delete one by one (including dependencies)
                    var objectManager = new AmaceObjectManager(director);
                    foreach (var rhobj in selectedReamingBlocks)
                    {
                        objectManager.DeleteObject(rhobj.Id);
                    }

                    // Stop user input
                    break;
                }
                if (result == DialogResult.Cancel)
                {
                    return Result.Failure;
                }
            }

            // Success
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            // Dependencies
            _dependencies.DeleteBlockDependencies(director, IBB.ExtraReamingEntity);
            _dependencies.UpdateAdditionalReaming(director);
            // Set visibility
            Visibility.ReamingDefault(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            Visibility.ReamingDefault(doc);
        }
    }
}