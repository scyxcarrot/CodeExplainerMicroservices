using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Glenius.Operations
{
    public class PreopImporter
    {
        public Dictionary<IBB, string> BlockToKeywordMapping { get; private set; }

        public bool ImportData(GleniusImplantDirector director, List<GleniusImportFileName> fileToImport)
        {
            var objectManager = new GleniusObjectManager(director);

            var importSuccessful = true;
            foreach (var file in fileToImport)
            {
                Mesh mesh;
                bool read = StlUtilities.StlBinary2RhinoMesh(file.FullPath, out mesh);
                if (!read)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Something went wrong while reading the STL file: {0}", file.FullName);
                    importSuccessful = false;
                }
                file.SetImportedMesh(mesh);
            }

            if (importSuccessful)
            {
                BlockToKeywordMapping = new Dictionary<IBB, string>();
                foreach (var file in fileToImport)
                {
                    var buildingBlock = file.BuildingBlock.Value;
                    objectManager.AddNewBuildingBlock(buildingBlock, file.ImportedMesh);
                    BlockToKeywordMapping.Add(buildingBlock, file.Keyword);
                }
            }

            return importSuccessful;
        }
    }
}