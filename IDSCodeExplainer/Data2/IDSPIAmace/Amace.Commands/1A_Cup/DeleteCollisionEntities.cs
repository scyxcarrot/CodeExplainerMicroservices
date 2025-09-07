using IDS.Amace.Enumerators;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Operations;
using Rhino;
using Rhino.Commands;

namespace IDS.Amace.Commands
{
    /**
     * Rhino command to delete collision entities
     */

    [System.Runtime.InteropServices.Guid("DCE97CC4-961A-447A-8C5F-F5D78665DA55")]
    [IDSCommandAttributes(true, DesignPhase.Cup | DesignPhase.Screws)]
    public class DeleteCollisionEntities : CommandBase<ImplantDirector>
    {
        /**
         * Initialize singleton instance representing this command.
         */

        public DeleteCollisionEntities()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /** The one and only instance of this command */

        public static DeleteCollisionEntities TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "DeleteCollisionEntities";

        /**
         * Run the command choose collision entities and delete them
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Set visualization for deleting collision entities
            Visibility.CupDefault(doc);

            // Unlock collision entities for selection
            Operations.Locking.UnlockCollisionEntities(doc);

            var success = EntitiesDeleter.DeleteEntities("Select collision entities to remove.",
                "Are you sure you want to delete the selected collision entit(y/ies)?",
                "Delete Entit(y/ies)?", director);
            return success ? Result.Success : Result.Failure;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            // Lock all and redraw
            if (director.CurrentDesignPhase == DesignPhase.Cup)
            {
                Visibility.CupDefault(doc);
            }
            else
            {
                Visibility.ScrewDefault(doc);
            }
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            // Lock all and redraw
            if (director.CurrentDesignPhase == DesignPhase.Cup)
            {
                Visibility.CupDefault(doc);
            }
            else
            {
                Visibility.ScrewDefault(doc);
            }
        }
    }
}