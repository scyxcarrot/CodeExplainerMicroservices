using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Query
{
    public class QCDocumentOverviewQuery
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;

        public QCDocumentOverviewQuery(CMFImplantDirector director)
        {
            this._director = director;
            _objectManager = new CMFObjectManager(director);
        }

        public bool HasImplantPreview()
        {
            return HasImplantComponent(IBB.ImplantPreview);
        }

        public bool HasGuidePreviewSmoothen()
        {
            return HasGuideComponent(IBB.GuidePreviewSmoothen);
        }

        public bool HasActualImplant()
        {
            return HasImplantComponent(IBB.ActualImplant);
        }

        public bool HasActualGuide()
        {
            return HasGuideComponent(IBB.ActualGuide);
        }

        private bool HasImplantComponent(IBB component)
        {
            var exist = false;

            var implantComponent = new ImplantCaseComponent();

            foreach (var casePreferenceData in _director.CasePrefManager.CasePreferences)
            {
                var extendedBuildingBlock = implantComponent.GetImplantBuildingBlock(component, casePreferenceData);
                if (!_objectManager.HasBuildingBlock(extendedBuildingBlock))
                {
                    continue;
                }
                exist = true;
                break;
            }

            return exist;
        }

        private bool HasGuideComponent(IBB component)
        {
            var exist = false;

            var guideComponent = new GuideCaseComponent();

            foreach (var guidePreference in _director.CasePrefManager.GuidePreferences)
            {
                var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(component, guidePreference);
                if (!_objectManager.HasBuildingBlock(extendedBuildingBlock))
                {
                    continue;
                }
                exist = true;
                break;
            }

            return exist;
        }
    }
}
