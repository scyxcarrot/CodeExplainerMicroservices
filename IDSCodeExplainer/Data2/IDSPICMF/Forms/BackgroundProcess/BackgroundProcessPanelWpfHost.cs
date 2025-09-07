using Rhino;
using Rhino.UI;
using System;

namespace IDS.PICMF.Forms.BackgroundProcess
{
    [System.Runtime.InteropServices.Guid("C9AEF0F1-2128-4262-8525-560887856AF0")]
    public class BackgroundProcessPanelWpfHost : RhinoWindows.Controls.WpfElementHost
    {
        public BackgroundProcessPanelWpfHost() : 
            base(new BackgroundProcessPanel(), null)
        {
            Enabled = true;
        }

        public static Guid PanelId => typeof(BackgroundProcessPanelWpfHost).GUID;

        public static void OpenPanel()
        {
            var panelId = PanelId;
            var panelVisible = Panels.IsPanelVisible(panelId);
            if (!panelVisible)
            {
                Panels.OpenPanel(panelId, true);
                Panels.OnShowPanel(panelId, RhinoDoc.ActiveDoc.RuntimeSerialNumber, true);
            }
        }

        public static BackgroundProcessPanelViewModel GetViewModel()
        {
            var panel = Panels.GetPanel<BackgroundProcessPanelWpfHost>(RhinoDoc.ActiveDoc);
            return (panel?.FrameworkElement as BackgroundProcessPanel)?.ViewModel;
        }
    }
}
