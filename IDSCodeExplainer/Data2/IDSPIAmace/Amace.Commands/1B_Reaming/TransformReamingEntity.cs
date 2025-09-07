using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Operations;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Linq;
using Locking = IDS.Amace.Operations.Locking;


namespace IDS.Commands.Reaming
{
    /**
     * Rhino command to make a cup screw.
     */

    [System.Runtime.InteropServices.Guid("B01D6765-23A3-4E59-9EB8-885DEFB2426B")]
    [IDSCommandAttributes(true, DesignPhase.Reaming, IBB.Cup)]
    public class TransformReamingEntity : CommandBase<ImplantDirector>
    {
        /**
         * Initialize singleton instance representing this command.
         */

        public TransformReamingEntity()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /** The one and only instance of this command */

        public static TransformReamingEntity TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "TransformReamingEntity";

        /**
         * Run the command to make a screw.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Set visibility
            Visibility.ReamingDefaultWithoutCupRbv(doc);
            // Unlock reaming entities
            Locking.UnlockReamingEntities(doc);
            // Get entity
            var selectReamingBlocks = new GetObject();
            selectReamingBlocks.SetCommandPrompt("Select reaming entity to transform.");
            selectReamingBlocks.EnablePreSelect(false, false);
            selectReamingBlocks.EnablePostSelect(true);
            selectReamingBlocks.AcceptNothing(true);
            // Get user input
            var res = selectReamingBlocks.Get();

            if (res == GetResult.Nothing || res == GetResult.Cancel)
            {
                return Result.Failure;
            }

            if (res == GetResult.Object)
            {
                // Get selected objects
                var selectedReamingBlocks = doc.Objects.GetSelectedObjects(false, false).ToList();
                // Set visibility
                Visibility.ReamingDefaultWithoutCupRbv(doc);

                // Transform object
                var rhobj = selectedReamingBlocks[0];
                var gTransform = new GumballTransformBrep(doc, false);
                var objectTransform = gTransform.TransformBrep(rhobj.Id);
                if (objectTransform == Transform.Identity)
                {
                    return Result.Failure;
                }
            }

            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            // Dependencies
            var dep = new Dependencies();
            dep.DeleteBlockDependencies(director, IBB.ExtraReamingEntity);
            dep.UpdateAdditionalReaming(director);
            // Set visibility
            Visibility.ReamingDefault(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            Visibility.ReamingDefault(doc);
        }
    }
}