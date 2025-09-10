using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Invalidation
{
    public class ImplantSupportTeethIntegrationInvalidator : IInvalidator
    {
        private readonly CMFImplantDirector _director;

        public ImplantSupportTeethIntegrationInvalidator(CMFImplantDirector director)
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
            //1. Has teeth integration
            //2. Input has Planned teeth => invalidate implant support teeth integration roi and reset data model's values

            var partsToInvalidate = new List<PartProperties>();

            var objectManager = new CMFObjectManager(_director);
            var implantSupportTeethIntegrationRoI = objectManager.GetBuildingBlock(IBB.ImplantSupportTeethIntegrationRoI);
            if (implantSupportTeethIntegrationRoI == null)
            {
                return partsToInvalidate;
            }

            var hasPlannedTeeth = partsThatChanged.Any(p => IsPlannedTeeth(p.Name));

            if (!hasPlannedTeeth)
            {
                return partsToInvalidate;
            }

            partsToInvalidate.Add(new PartProperties(implantSupportTeethIntegrationRoI.Id, implantSupportTeethIntegrationRoI.Name, IBB.ImplantSupportTeethIntegrationRoI));
            _director.ImplantManager.ResetImplantSupportRoITeethIntegrationInformation();

            //invalidate
            foreach (var part in partsToInvalidate)
            {
                objectManager.DeleteObject(part.Id);
            }

            // Invalidate all implant support if there are imported teeth
            OutdatedImplantSupportHelper.SetAllImplantSupportsOutdated(_director);

            return partsToInvalidate;
        }

        private bool IsPlannedTeeth(string partName)
        {
            return ImportRecutInvalidationUtilities.IsPartOf(partName, ProplanBoneType.Planned, ProPlanImportPartType.Teeth);
        }
    }
}
