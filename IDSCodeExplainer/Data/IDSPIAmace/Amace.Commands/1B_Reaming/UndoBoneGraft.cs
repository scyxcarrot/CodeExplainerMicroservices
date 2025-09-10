using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDS.Amace.Commands
{
    /// <summary>
    /// Import a bone graft and the corresponding STL of the merged bone and bone graft
    /// </summary>
    /// <seealso cref="Rhino.Commands.Command" />
    [System.Runtime.InteropServices.Guid("501E2C71-6CC4-42BA-BA1E-C25423B3B43B")]
    [IDSCommandAttributes(false, DesignPhase.Reaming, IBB.DesignPelvis, IBB.BoneGraft)]
    public class UndoBoneGraft: CommandBase<ImplantDirector>
    {
        public UndoBoneGraft()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static UndoBoneGraft TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "UndoBoneGraft";

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
            // Remove existing graft
            var objectManager = new AmaceObjectManager(director);
            objectManager.DeleteObject(objectManager.GetBuildingBlockId(IBB.BoneGraft));
            var dependencies = new Dependencies();
            dependencies.DeleteBlockDependencies(director, IBB.BoneGraft);

            // Replace defect and design pelvis with preop pelvis
            var preopPelvis = objectManager.GetBuildingBlock(IBB.PreopPelvis).Geometry as Mesh;
            objectManager.SetBuildingBlock(IBB.DefectPelvis, preopPelvis, objectManager.GetBuildingBlockId(IBB.DefectPelvis));
            objectManager.SetBuildingBlock(IBB.DesignPelvis, preopPelvis, objectManager.GetBuildingBlockId(IBB.DesignPelvis));
            dependencies.DeleteBlockDependencies(director, IBB.DesignPelvis);

            // Update reaming
            dependencies.UpdateCupAndAdditionalReaming(director);

            Visibility.ReamingDefault(doc);

            // Success!
            return Result.Success;
        }
    }
}