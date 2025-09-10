using IDS.CMF;
using IDS.CMF.Query;
using IDS.PICMF.Forms;
using System.Linq;

namespace IDS.PICMF.Visualization
{
    public static class PreferencePanelHelper
    {
        public static bool NumberPresentInImplant(int caseNumber)
        {
            var implantPrefModels = CasePreferencePanel.GetView().GetViewModel().
                GetAllCasePreferenceControls().Select(x => x.ViewModel.Model).ToList();

            return implantPrefModels.Exists(x => x.CaseNumber == caseNumber);
        }

        public static bool NumberPresentInGuide(int caseNumber)
        {
            var guidePrefModels = CasePreferencePanel.GetView().GetViewModel().
                GetAllGuidePreferenceControls().Select(x => x.ViewModel.Model).ToList();

            return guidePrefModels.Exists(x => x.CaseNumber == caseNumber);
        }

        public static void InvalidateAllLinkedImplantDisplayStringOnGuidePreferences(CMFImplantDirector director)
        {
            var guidePrefModels = CasePreferencePanel.GetView().GetViewModel().
                GetAllGuidePreferenceControls().Select(x => x.ViewModel.Model).ToList();
            ImplantGuideLinkQuery.SetLinkedImplantsDisplayString(director, ref guidePrefModels);
        }

        public static void InvalidateAllLinkedGuideDisplayStringOnImplantPreferences(CMFImplantDirector director)
        {
            var implantPrefModels = CasePreferencePanel.GetView().GetViewModel().
                GetAllCasePreferenceControls().Select(x => x.ViewModel.Model).ToList();
            ImplantGuideLinkQuery.SetLinkedGuidesDisplayString(director, ref implantPrefModels);
        }
    }
}
