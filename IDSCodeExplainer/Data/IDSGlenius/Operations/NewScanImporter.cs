using IDS.Core.Enumerators;
using IDS.Core.Importer;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class NewScanImporter
    {
        public Dictionary<IBB, string> BlockToKeywordMapping { get; private set; }

        public Plane AxialPlane { get; private set; }

        public List<GleniusImportFileName> FileInfos { get; private set; }

        public bool ImportDataToMemory(string folderPath)
        {
            var dataProvider = new NewScanDataProvider();
            FileInfos = dataProvider.GetSTLFileInfos(folderPath);

            var importSuccessful = true;
            foreach (var file in FileInfos)
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
            
            var axialPlanePath = dataProvider.GetAxialPlanePath(folderPath);

            // import plane
            Plane axialPlane;
            if (!PlaneImporter.ImportXMLPlane(axialPlanePath, out axialPlane))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Something went wrong while reading the XML file of Axial Plane");
                importSuccessful = false;
            }
            AxialPlane = axialPlane;

            if (importSuccessful)
            {
                BlockToKeywordMapping = new Dictionary<IBB, string>();
                foreach (var file in FileInfos)
                {
                    var buildingBlock = file.BuildingBlock.Value;
                    BlockToKeywordMapping.Add(buildingBlock, file.Keyword);
                }
            }

            return importSuccessful;
        }

        public bool AddDataToDocument(GleniusImplantDirector director)
        {
            var importSuccessful = FileInfos != null && FileInfos.TrueForAll(info => info.ImportedMesh != null) && AxialPlane != null;

            if (importSuccessful)
            {
                var objectManager = new GleniusObjectManager(director);

                foreach (var file in FileInfos)
                {
                    var buildingBlock = file.BuildingBlock.Value;

                    var buildingBlockIds = objectManager.GetAllBuildingBlockIds(buildingBlock);

                    if (buildingBlockIds.Any())
                    {
                        foreach (var oldId in objectManager.GetAllBuildingBlockIds(buildingBlock))
                        {
                            objectManager.SetBuildingBlock(buildingBlock, file.ImportedMesh, oldId);
                        }
                    }
                    else
                    {
                        objectManager.AddNewBuildingBlock(buildingBlock, file.ImportedMesh);
                    }
                }
            }

            return importSuccessful;
        }

        //Returns null if none is found
        public Mesh GetMesh(IBB meshOfIBB)
        {
            var res = FileInfos.SingleOrDefault(x => x.BuildingBlock == meshOfIBB);
            return res?.ImportedMesh;
        }
    }
}