using IDS.PICMF.Forms;
using System.Collections.Generic;
using System.Windows.Controls;

namespace IDS.PICMF.Visualization
{
    public static class UiUtilities
    {
        public static void InvalidateUserControlWidth(ref UserControl uc)
        {
            if (uc == null)
            {
                return;
            }

            var view = CasePreferencePanel.GetView();
            uc.Width = view.ActualWidth - 20;
        }

        public static void SubscribePanelWidthInvalidation()
        {

            var view = CasePreferencePanel.GetView();
            view.SizeChanged -= View_SizeChanged; //Remove old one, if any
            view.SizeChanged += View_SizeChanged;
        }

        private static void View_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            var vm = CasePreferencePanel.GetPanelViewModel();

            var infoOnSurgeryUc = (UserControl)vm.InfoOnSurgeryControl;
            UiUtilities.InvalidateUserControlWidth(ref infoOnSurgeryUc);

            var allUcs = new List<UserControl>();
            allUcs.AddRange(vm.ListSurgeryInfoItems);
            allUcs.AddRange(vm.GetAllCasePreferenceControls());
            allUcs.AddRange(vm.GetAllGuidePreferenceControls());

            allUcs.ForEach(x =>
            {
                UiUtilities.InvalidateUserControlWidth(ref x);
            });
        }
    }

}
