using System;

namespace IDS.Amace.GUI
{
    internal static class ScrewPanelHelper
    {
        public static void ShowScrewPanel(ImplantDirector director)
        {
            // Refresh and show the screw panel (if it is open)
            ScrewPanel screwPanel = ScrewPanel.GetPanel();
            if (null != screwPanel)
            {
                Guid panelId = ScrewPanel.panelId;
                if (screwPanel != null)
                {
                    screwPanel.RefreshPanelInfo();
                    Rhino.UI.Panels.OpenPanel(panelId);
                    screwPanel.Enabled = true;
                }
            }
        }
    }
}