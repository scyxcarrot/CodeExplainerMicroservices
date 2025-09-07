using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using System;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class PastilleDeformedChecker : ImplantScrewQcProxyChecker
    {
        private readonly CMFObjectManager _objectManager;

        public override string ScrewQcCheckTrackerName => "Pastille Deformed Check";

        public PastilleDeformedChecker(CMFImplantDirector director) :
            base(ImplantScrewQcCheck.PastilleDeformed)
        {
            _objectManager = new CMFObjectManager(director);
        }

        public override IScrewQcResult Check(Screw screw)
        {
            var casePreference = _objectManager.GetCasePreference(screw);
            return new PastilleDeformedResult(ScrewQcCheckName, PerformPastilleDeformedCheck(casePreference, screw.Id));
        }

        public PastilleDeformedContent PerformPastilleDeformedCheck(CasePreferenceDataModel casePreference, Guid screwId)
        {
            var dotPastille = casePreference.ImplantDataModel.DotList.FirstOrDefault(dot => (dot as DotPastille)?.Screw != null && screwId == (dot as DotPastille).Screw.Id);
            var content = new PastilleDeformedContent()
            {
                IsPastilleDeformed = ((DotPastille)dotPastille).CreationAlgoMethod != DotPastille.CreationAlgoMethods[0]
            };

            return content;
        }
    }
}
