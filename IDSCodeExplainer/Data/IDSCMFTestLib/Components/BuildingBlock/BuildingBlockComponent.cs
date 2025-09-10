using IDS.CMF.ImplantBuildingBlocks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Rhino.Geometry;

namespace IDS.CMF.TestLib.Components
{
    //Note: This component only handle single rhino object building block
    public class BuildingBlockComponent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public IBB BuildingBlock { get; set; }

        public MeshComponent BuildingBlockMesh { get; set; } = new MeshComponent();

        public void ParseToDirector(CMFObjectManager objectManager, string workDir)
        {
            BuildingBlockMesh.ParseFromComponent(workDir, out var partMesh);
            objectManager.AddNewBuildingBlock(BuildingBlock, partMesh);
        }

        public bool FillToComponent(CMFObjectManager objectManager, string workDir)
        {
            if (!objectManager.HasBuildingBlock(BuildingBlock))
            {
                return false;
            }

            var rhinoObject = objectManager.GetBuildingBlock(BuildingBlock);
            BuildingBlockMesh.FillToComponent($"{BuildingBlocks.Blocks[BuildingBlock].Name}.stl", workDir, (Mesh)rhinoObject.Geometry);
            return true;
        }
    }
}
