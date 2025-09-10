using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Importer;
using Rhino;
using Rhino.Commands;

namespace IDS.Amace.Commands
{
    /**
     * Rhino command to import an STL file and set it as a collidable entity
     */

    [System.Runtime.InteropServices.Guid("D53285D4-7325-4FB3-9D51-1BBA129E9F4F")]
    [IDSCommandAttributes(true, DesignPhase.Cup | DesignPhase.Screws)]
    public class ImportCollisionEntities : CommandBase<ImplantDirector>
    {
        public ImportCollisionEntities()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static ImportCollisionEntities TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "ImportCollisionEntities";

        /**
         * Import an STL file and set it as a collidable entity
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            var meshes = StlImporter.ImportStl();
            if (meshes == null)
            {
                return Result.Failure;
            }

            var objectManager = new AmaceObjectManager(director);
            foreach (var blockMesh in meshes)
            {
                objectManager.AddNewBuildingBlock(IBB.CollisionEntity, blockMesh);
            }

            return Result.Success;
        }


        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            // Set visualization
            if (director.CurrentDesignPhase == DesignPhase.Cup)
            {
                Visibility.CupDefault(doc);
            }
            else if (director.CurrentDesignPhase == DesignPhase.Screws)
            {
                Visibility.ScrewDefault(doc);
            }
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            // Set visualization
            if (director.CurrentDesignPhase == DesignPhase.Cup)
            {
                Visibility.CupDefault(doc);
            }
            else if (director.CurrentDesignPhase == DesignPhase.Screws)
            {
                Visibility.ScrewDefault(doc);
            }
        }
    }
}