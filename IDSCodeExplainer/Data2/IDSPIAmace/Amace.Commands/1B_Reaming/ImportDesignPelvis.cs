using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.Operations;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;

namespace IDS.Commands.Reaming
{
    /**
     * Import an stl as the new design pelvis
     */

    [System.Runtime.InteropServices.Guid("4A199C0A-B387-4E00-AE38-DAEEBCBD0D2A")]
    [IDSCommandAttributes(false, DesignPhase.Reaming, IBB.DesignPelvis)]
    public class ImportDesignPelvis : CommandBase<ImplantDirector>
    {
        public ImportDesignPelvis()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static ImportDesignPelvis TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "ImportDesignPelvis";

        /**
         * Import an stl as the new design pelvis
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Show file dialog
            var fd = new Rhino.UI.OpenFileDialog
            {
                Title = "Please select STL file containing mesh",
                Filter = "STL files (*.stl)|*.stl||",
                InitialDirectory = Environment.SpecialFolder.Desktop.ToString()
            };
            var drc = fd.ShowDialog();
            if (drc != System.Windows.Forms.DialogResult.OK)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Invalid or no file was chosen");
                return Result.Failure;
            }
            var stlFile = fd.FileName;

            // Import the mesh
            Mesh designPelvis;
            var read = StlUtilities.StlBinary2RhinoMesh(stlFile, out designPelvis);
            if (!read)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Something went wrong while reading the STL file");
                return Result.Failure;
            }

            // Set it as building block
            var objManager = new AmaceObjectManager(director);
            var designPelvisId = objManager.GetBuildingBlockId(IBB.DesignPelvis);
            objManager.SetBuildingBlock(IBB.DesignPelvis, designPelvis, designPelvisId);
            // Delete dependencies
            var dep = new Dependencies();
            dep.DeleteBlockDependencies(director, IBB.DesignPelvis);

            // Create reaming manager
            var reamingManager = new ReamingManager(director);

            // Perform cup reaming
            var performedCupReaming = reamingManager.PerformCupReaming(IBB.DesignPelvis);
            if (!performedCupReaming)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Cup reaming could not be performed.");
                return Result.Failure;
            }

            // Do additional reaming if any blocks are available
            var performedAdditionalReaming = reamingManager.PerformAdditionalReaming(IBB.CupReamedPelvis);
            if (!performedAdditionalReaming)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Additional reaming could not be performed.");
                return Result.Failure;
            }

            // Create design pelvis difference mesh

            // Get defect and design pelvis
            var designMeshDifference = AnalysisMeshMaker.CreateDesignMeshDifference(
                objManager.GetBuildingBlock(IBB.DesignPelvis).Geometry as Mesh,
                objManager.GetBuildingBlock(IBB.DefectPelvis).Geometry as Mesh);
            if (designMeshDifference == null)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Mesh difference (design vs defect) could not be calculated.");
                return Result.Failure;
            }
            // Set it as building block
            var meshDiffId = objManager.GetBuildingBlockId(IBB.DesignMeshDifference);
            objManager.SetBuildingBlock(IBB.DesignMeshDifference, designMeshDifference, meshDiffId);

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