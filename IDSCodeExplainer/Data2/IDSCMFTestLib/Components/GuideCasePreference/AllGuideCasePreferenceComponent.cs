using System.Collections.Generic;

namespace IDS.CMF.TestLib.Components
{
    public class AllGuideCasePreferenceComponent
    {
        public List<GuideCasePreferenceComponent> GuideCasePreferences { get; set; } =
            new List<GuideCasePreferenceComponent>();

        public void ParseToDirector(CMFImplantDirector director)
        {
            foreach (var guideCasePreferenceComponent in GuideCasePreferences)
            {
                guideCasePreferenceComponent.ParseToDirector(director);
            }
        }

        public void FillToComponent(CMFImplantDirector director)
        {
            foreach (var guidePreferenceDataModel in director.CasePrefManager.GuidePreferences)
            {
                var guideCasePreferences = new GuideCasePreferenceComponent();
                guideCasePreferences.FillToComponent(guidePreferenceDataModel);
                GuideCasePreferences.Add(guideCasePreferences);
            }
        }
    }
}
