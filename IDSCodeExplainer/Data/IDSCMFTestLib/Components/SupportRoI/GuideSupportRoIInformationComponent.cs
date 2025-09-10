using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using System.Collections.Generic;

namespace IDS.CMF.TestLib.Components
{
    public class GuideSupportRoIInformationComponent
    {
        public GuideSupportRoICreationData GuideSupportRoIInformation { get; set; } = new GuideSupportRoICreationData();

        public List<BuildingBlockComponent> Meshes { get; set; } = new List<BuildingBlockComponent>();

        private readonly List<IBB> _buildingBlockList = new List<IBB>
        {
            IBB.GuideSupportRoI,
            IBB.GuideSupportRemovedMetalIntegrationRoI
        };

        public void ParseToDirector(CMFImplantDirector director, string workDir)
        {
            var objectManager = new CMFObjectManager(director);

            foreach (var mesh in Meshes)
            {
                mesh.ParseToDirector(objectManager, workDir);
            }

            director.GuideManager.SetGuideSupportRoICreationInformation(GuideSupportRoIInformation);
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

            GuideSupportRoIInformation = director.GuideManager.GetGuideSupportRoICreationDataModel();
        }
    }
}
