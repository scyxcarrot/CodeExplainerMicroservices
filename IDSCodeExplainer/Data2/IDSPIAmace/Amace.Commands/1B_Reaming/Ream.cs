using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using Rhino;
using Rhino.Commands;

namespace IDS.Amace.Commands
{
    /**
     * Command to ream the defect pelvis using the skirt in order
     * to prepare the pelvis for wrapping (to define bottom surface
     * (of plate).
     */

    [System.Runtime.InteropServices.Guid("F5485B01-A558-47DC-BB9E-52ADEE3A9EBE")]
    [IDSCommandAttributes(false, DesignPhase.Reaming, IBB.Cup, IBB.DesignPelvis)]
    public class Ream : CommandBase<ImplantDirector>
    {
        public Ream()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        ///<summary>The one and only instance of this command</summary>
        public static Ream TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "Ream";

        private readonly Dependencies _dependencies;

        /**
         * Let user define surfaces for reaming the pelvis.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Do reaming
            var reamingManager = new ReamingManager(director);
            var success = reamingManager.PerformAdditionalReaming(IBB.CupReamedPelvis);
            if (!success)
            {
                return Result.Failure;
            }

            // Reached end: success!
            doc.Views.Redraw();
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            // Delete dependencies
            _dependencies.DeleteBlockDependencies(director, IBB.ReamedPelvis);

            // Set visibility
            Visibility.ReamingDefault(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            doc.Views.Redraw();
        }
    }
}