using System.Collections.Generic;

namespace IDS.CMF.TestLib.Components
{
    public class AllImplantCasePreferenceComponent
    {
        public List<ImplantCasePreferenceComponent> ImplantCasePreferences { get; set; } =
            new List<ImplantCasePreferenceComponent>();

        public void ParseToDirector(CMFImplantDirector director)
        {
            foreach (var implantCasePreferenceComponent in ImplantCasePreferences)
            {
                implantCasePreferenceComponent.ParseToDirector(director);
            }
        }

        public void FillToComponent(CMFImplantDirector director)
        {
            foreach (var implantCasePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var implantCasePreferences = new ImplantCasePreferenceComponent();
                implantCasePreferences.FillToComponent(implantCasePreferenceDataModel);
                ImplantCasePreferences.Add(implantCasePreferences);
            }
        }
    }
}
