using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;

namespace IDS.Amace.Commands
{
    /// <summary>
    /// Import a bone graft and the corresponding STL of the merged bone and bone graft
    /// </summary>
    /// <seealso cref="Rhino.Commands.Command" />
    [System.Runtime.InteropServices.Guid("3FD52278-8D6C-4F72-96FA-DE798E92E369")]
    [IDSCommandAttributes(false, DesignPhase.Reaming, IBB.DesignPelvis)]
    public class ImportBoneGraft: CommandBase<ImplantDirector>
    {
        public ImportBoneGraft()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static ImportBoneGraft TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "ImportBoneGraft";

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="doc">The current document.</param>
        /// <param name="mode">The command running mode.</param>
        /// <param name="director"></param>
        /// <returns>
        /// The command result code.
        /// </returns>
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Import meshes
            Mesh graft;
            var importedGraft = ImportMeshFromStlFile("Import Bone Graft STL.", out graft);
            if (!importedGraft)
            {
                return Result.Failure;
            }

            Mesh boneAndGraft;
            var importedBoneAndGraft = ImportMeshFromStlFile("Import Pelvis and Bone Graft STL.", out boneAndGraft);
            if (!importedBoneAndGraft)
            {
                return Result.Failure;
            }

            // Set building blocks
            var objectManager = new AmaceObjectManager(director);
            objectManager.SetBuildingBlock(IBB.BoneGraft, graft, objectManager.GetBuildingBlockId(IBB.BoneGraft));
            objectManager.SetBuildingBlock(IBB.DefectPelvis, boneAndGraft, objectManager.GetBuildingBlockId(IBB.DefectPelvis));

            // Delete dependencies
            var dependencies = new Dependencies();
            dependencies.DeleteBlockDependencies(director, IBB.DesignPelvis);
            dependencies.DeleteBlockDependencies(director, IBB.BoneGraft);
            // Update dependencies
            objectManager.SetBuildingBlock(IBB.DesignPelvis, boneAndGraft, objectManager.GetBuildingBlockId(IBB.DesignPelvis));
            dependencies.UpdateCupAndAdditionalReaming(director);

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

        private static bool ImportMeshFromStlFile(string message, out Mesh mesh)
        {
            mesh = null;

            // File dialog
            var fd = new Rhino.UI.OpenFileDialog
            {
                Title = message,
                Filter = "STL files (*.stl)|*.stl||",
                InitialDirectory = Environment.SpecialFolder.Desktop.ToString()
            };
            var drc = fd.ShowDialog();

            // Read if 
            if (drc != System.Windows.Forms.DialogResult.OK)
            {
                return false;
            }

            var stlFile = fd.FileName;

            // Import the mesh
            return StlUtilities.StlBinary2RhinoMesh(stlFile, out mesh);
        }
    }
}