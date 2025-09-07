using Rhino;
using Rhino.PlugIns;
using Rhino.UI;
using System;
using System.Drawing;
using IDS.Glenius.Forms;

namespace IDS.Glenius
{
    public class IDSPanel
    {
        private PlugIn plugin;

        public IDSPanel(PlugIn plugin)
        {
            this.plugin = plugin;
        }

        /// <summary>
        /// Register all panels provided by this plug-in with the Rhino application.
        /// </summary>
        public void RegisterIDSPanels()
        {
            RegisterGleniusPanels();
        }

        #region Glenius
        private void RegisterGleniusPanels()
        {
            RegisterHeadPanel();
        }

        private void RegisterHeadPanel()
        {
            Type headPanelType = typeof(HeadPanelWpfHost);
            var resources = new Resources(); //[AH] Icon?
            Panels.RegisterPanel(plugin, headPanelType, "Head", new Icon(resources.HeadPanelIcon));
        }
        #endregion

    }
}