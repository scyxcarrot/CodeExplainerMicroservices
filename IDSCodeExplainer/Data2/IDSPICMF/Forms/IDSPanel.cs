using IDS.PICMF.Forms.BackgroundProcess;
using Rhino.PlugIns;
using Rhino.UI;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace IDS.PICMF.Forms
{
    public class IDSPanel
    {
        private readonly PlugIn _plugin;

        public IDSPanel(PlugIn plugin)
        {
            this._plugin = plugin;
        }

        public void RegisterIDSPanels()
        {
            RegisterCmfPanels();
        }

        private void RegisterCmfPanels()
        {
            RegisterCasePreferencePanel();
        }

        private void RegisterCasePreferencePanel()
        {
            //TODO: USE CORRECT ICON
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "Assets");
            path = Path.Combine(path, "CupPanelIcon.ico");

            var headPanelType = typeof(CasePreferencePanelWpfHost);
            Panels.RegisterPanel(_plugin, headPanelType, "IDS - Case Preference Panel", new Icon(path), PanelType.System);
#if (INTERNAL)
            path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "Assets");
            path = Path.Combine(path, "BackgroundProcess.ico");

            var backgroundProcessPanelType = typeof(BackgroundProcessPanelWpfHost);
            Panels.RegisterPanel(_plugin, backgroundProcessPanelType, "IDS - Background Process Panel", new Icon(path), PanelType.System);
#endif
            path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "Assets");
            path = Path.Combine(path, "TeethBlockWizardPanel.ico");

            var teethBlockWizardPanelType = typeof(TeethBlockWizardPanelWpfHost);
            Panels.RegisterPanel(_plugin, teethBlockWizardPanelType, "IDS - Teeth Block Wizard Panel", new Icon(path), PanelType.System);
        }
    }
}
