using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Amace.Commands
{
    /**
     * Rhino command to make a cup screw.
     */

    [System.Runtime.InteropServices.Guid("48D0802E-489A-4EF8-9695-D0C61C6CD6E6")]
    [IDSCommandAttributes(true, DesignPhase.Skirt, IBB.SkirtGuide)]
    public class DeleteGuides : CommandBase<ImplantDirector>
    {
        /**
         * Initialize singleton instance representing this command.
         */
        public DeleteGuides()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /** The one and only instance of this command */

        public static DeleteGuides TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "DeleteGuides";

        /**
         * Run the command to make a screw.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {

            // Unlock screws
            Locking.UnlockSkirtGuides(director.Document);
            // Get screw
            var select = new GetObject();
            select.SetCommandPrompt("Select guide(s) to remove. Press ENTER without selecting any to delete all.");
            select.EnablePreSelect(false, false);
            select.EnablePostSelect(true);
            select.AcceptNothing(true);

            var objectManager = new ObjectManager(director);

            // Get user input
            while (true)
            {
                var res = select.GetMultiple(0, 0);

                if (res == GetResult.Cancel)
                {
                    return Result.Failure;
                }

                if (res == GetResult.Nothing)
                {
                    // Ask confirmation and delete if user clicks 'Yes'
                    Rhino.UI.Dialogs.ShowMessageBox(
                        "Are you sure you want to delete all guide(s)?",
                        "Delete guide(s)?",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Exclamation);

                    // Delete all guides
                    var filter = new ObjectEnumeratorSettings();
                    filter.NormalObjects = true;
                    filter.LockedObjects = false;
                    filter.HiddenObjects = false;
                    var unlocked = doc.Objects.GetObjectList(filter).ToList();
                    foreach (var rhobj in unlocked)
                    {
                        objectManager.DeleteObject(rhobj.Id);
                    }
                    break;
                }

                if (res != GetResult.Object)
                {
                    return Result.Failure;
                }

                if (HandleObjectType(doc, objectManager))
                {
                    break;
                }

                return Result.Failure;
            }
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            Visibility.SkirtDefault(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            Visibility.SkirtDefault(doc);
        }

        private bool HandleObjectType(RhinoDoc doc, ObjectManager objManager)
        {
            // Ask confirmation and delete if user clicks 'Yes'
            var result = Rhino.UI.Dialogs.ShowMessageBox(
                "Are you sure you want to delete the selected guide(s)?",
                "Delete guide(s)?",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Exclamation);
            switch (result)
            {
                case DialogResult.Yes:
                    // Get selected objects
                    var selected = doc.Objects.GetSelectedObjects(false, false).ToList();
                    // Delete one by one (including dependencies)
                    foreach (var rhobj in selected)
                    {
                        objManager.DeleteObject(rhobj.Id);
                    }

                    // Stop user input
                    return true;
                case DialogResult.Cancel:
                    return false;
                default:
                    return false;
            }
        }

    }
}