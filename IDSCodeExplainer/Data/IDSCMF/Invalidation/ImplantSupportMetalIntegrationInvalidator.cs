using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Invalidation
{
    public class ImplantSupportMetalIntegrationInvalidator : IInvalidator
    {
        private readonly CMFImplantDirector _director;

        public ImplantSupportMetalIntegrationInvalidator(CMFImplantDirector director)
        {
            _director = director;
        }

        public void SetInternalGraph()
        {
            //do nothing
        }

        public List<PartProperties> Invalidate(List<PartProperties> partsThatChanged)
        {
            //check parts
            //1. Has metal integration
            //2. Input has Planned metal => invalidate implant support metal integration roi and reset data model's values

            var partsToInvalidate = new List<PartProperties>();

            var objectManager = new CMFObjectManager(_director);
            var removedMetal = objectManager.GetBuildingBlock(IBB.ImplantSupportRemovedMetalIntegrationRoI);
            var remainedMetal = objectManager.GetBuildingBlock(IBB.ImplantSupportRemainedMetalIntegrationRoI);
            if (removedMetal == null && remainedMetal == null)
            {
                return partsToInvalidate;
            }

            var hasPlannedMetal = partsThatChanged.Any(p => IsPlannedMetal(p.Name));

            if (!hasPlannedMetal)
            {
                return partsToInvalidate;
            }

            if (removedMetal != null)
            {
                partsToInvalidate.Add(new PartProperties(removedMetal.Id, removedMetal.Name, IBB.ImplantSupportRemovedMetalIntegrationRoI));
            }

            if (remainedMetal != null)
            {
                partsToInvalidate.Add(new PartProperties(remainedMetal.Id, remainedMetal.Name, IBB.ImplantSupportRemainedMetalIntegrationRoI));
            }

            _director.ImplantManager.ResetImplantSupportRoIMetalIntegrationInformation();

            //invalidate
            foreach (var part in partsToInvalidate)
            {
                objectManager.DeleteObject(part.Id);
            }

            // Invalidate all implant support if there are imported metal integration
            OutdatedImplantSupportHelper.SetAllImplantSupportsOutdated(_director);

            return partsToInvalidate;
        }

        private bool IsPlannedMetal(string partName)
        {
            return ImportRecutInvalidationUtilities.IsPartOf(partName, ProplanBoneType.Planned, ProPlanImportPartType.Metal);
        }
    }
}
