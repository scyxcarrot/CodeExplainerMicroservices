using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Importer;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("893B9E6A-8B38-45A9-8751-A20C40076426")]
    [IDSGleniusCommand(~DesignPhase.Draft)]
    public class GleniusImportReferenceEntities : CommandBase<GleniusImplantDirector>
    {
        public GleniusImportReferenceEntities()
        {
            TheCommand = this;
            VisualizationComponent = new ImportReferenceEntitiesVisualization();
        }
        
        public static GleniusImportReferenceEntities TheCommand { get; private set; }

        public override string EnglishName => "GleniusImportReferenceEntities";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var meshes = StlImporter.ImportStl();
            if (meshes == null)
            {
                return Result.Failure;
            }

            var objectManager = new GleniusObjectManager(director);
            foreach (var mesh in meshes)
            {
                objectManager.AddNewBuildingBlock(IBB.ReferenceEntities, mesh);
            }

            return Result.Success;
        }
    }
}