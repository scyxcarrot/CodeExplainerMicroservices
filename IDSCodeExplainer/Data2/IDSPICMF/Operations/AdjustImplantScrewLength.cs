using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using System.Collections.Generic;

namespace IDS.PICMF.Operations
{
    public class AdjustImplantScrewLength : AdjustScrewLengthBase
    {

        public AdjustImplantScrewLength(Screw screw, List<double> availableLengths)
            : base(screw, availableLengths)
        {

        }

        protected override void UpdateScrew(Screw updatedScrew)
        {
            var objectManager = new CMFObjectManager(director);
            var casePreferenceData = objectManager.GetCasePreference(referenceScrew);

            var screwManager = new ScrewManager(director);
            screwManager.ReplaceExistingImplantScrewWithoutAnyInvalidation(updatedScrew, ref referenceScrew, casePreferenceData);
        }
    }
}