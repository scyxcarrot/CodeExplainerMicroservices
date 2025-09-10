using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDS.Amace.Commands
{
    /**
     * Import an stl as the new design pelvis
     */

    [System.Runtime.InteropServices.Guid("76AEC6E6-DE0E-4BC4-AC91-2374FC531DF6")]
    [IDSCommandAttributes(false, DesignPhase.Reaming, IBB.DesignPelvis)]
    public class UndoDesignPelvis : CommandBase<ImplantDirector>
    {
        public UndoDesignPelvis()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        ///<summary>The one and only instance of this command</summary>
        public static UndoDesignPelvis TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "UndoDesignPelvis";

        private readonly Dependencies _dependencies;

        /**
         * Import an stl as the new design pelvis
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            var objectManager = new AmaceObjectManager(director);

            // Set it as building block
            var designPelvisID = objectManager.GetBuildingBlockId(IBB.DesignPelvis);
            var defectPelvis = objectManager.GetBuildingBlock(IBB.DefectPelvis).Geometry as Mesh;
            objectManager.SetBuildingBlock(IBB.DesignPelvis, defectPelvis, designPelvisID);
            // Delete dependencies
            _dependencies.DeleteBlockDependencies(director, IBB.DesignPelvis);

            // Create reaming manager
            var reamingManager = new ReamingManager(director);

            // Perform cup reaming
            var success = reamingManager.PerformCupReaming(IBB.DesignPelvis);
            if (!success)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Cup reaming could not be performed.");
                return Result.Failure;
            }

            // Do additional reaming if any blocks are available
            success = reamingManager.PerformAdditionalReaming(IBB.CupReamedPelvis);
            if (!success)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Additional reaming could not be performed.");
                return Result.Failure;
            }

            // Delete design pelvis difference mesh
            var meshDiffId = objectManager.GetBuildingBlockId(IBB.DesignMeshDifference);
            objectManager.DeleteObject(meshDiffId);

            // Success!
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            Visibility.ReamingDefault(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            Visibility.ReamingDefault(doc);
        }
    }
}