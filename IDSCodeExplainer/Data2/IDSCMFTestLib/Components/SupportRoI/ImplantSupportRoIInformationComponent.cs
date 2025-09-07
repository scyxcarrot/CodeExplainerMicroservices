using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using System.Collections.Generic;

namespace IDS.CMF.TestLib.Components
{
    public class ImplantSupportRoIInformationComponent
    {
        public ImplantSupportRoICreationData ImplantSupportRoIInformation { get; set; } = new ImplantSupportRoICreationData();

        public List<BuildingBlockComponent> Meshes { get; set; } = new List<BuildingBlockComponent>();

        private readonly List<IBB> _buildingBlockList = new List<IBB>
        {
            IBB.ImplantSupportTeethIntegrationRoI,
            IBB.ImplantSupportRemovedMetalIntegrationRoI,
            IBB.ImplantSupportRemainedMetalIntegrationRoI
        };

        public void ParseToDirector(CMFImplantDirector director, string workDir)
        {
            var objectManager = new CMFObjectManager(director);

            foreach (var mesh in Meshes)
            {
                mesh.ParseToDirector(objectManager, workDir);
            }

            director.ImplantManager.SetImplantSupportRoICreationInformation(ImplantSupportRoIInformation);
        }

        public void FillToComponent(CMFImplantDirector director, string workDir)
        {
            var objectManager = new CMFObjectManager(director);

            foreach (var buildingBlock in _buildingBlockList)
            {
                var component = new BuildingBlockComponent
                {
                    BuildingBlock = buildingBlock
                };

                if (component.FillToComponent(objectManager, workDir))
                {
                    Meshes.Add(component);
                }
            }

            ImplantSupportRoIInformation = director.ImplantManager.GetImplantSupportRoICreationDataModel();
        }
    }
}
