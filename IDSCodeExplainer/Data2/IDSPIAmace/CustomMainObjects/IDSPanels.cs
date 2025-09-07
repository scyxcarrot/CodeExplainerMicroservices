using IDS.Amace;
using IDS.Amace.GUI;
using Rhino;
using Rhino.PlugIns;
using Rhino.UI;
using System;
using System.Drawing;

namespace IDS.Common
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
            RegisterAmacePanels();
        }

        public void InitializeIDSPanels(RhinoDoc document)
        {
            InitializeAmacePanels(document);
        }

        #region Amace

        private void RegisterAmacePanels()
        {
            // Register Cup Positioning Panel
            RegisterCupPanel();

            // Register screw panel
            RegisterScrewPanel();
        }

        private void InitializeAmacePanels(RhinoDoc document)
        {
            InitializeCupPanel(document);
            InitializeScrewPanel(document);
        }

        private void RegisterScrewPanel()
        {
            var screwPanelType = typeof(ScrewPanel);
            var resources = new AmaceResources();
            Panels.RegisterPanel(plugin, screwPanelType, "Screws", new Icon(resources.ScrewPanelIconFile));
        }

        private void RegisterCupPanel()
        {
            var cupPanelType = typeof(CupPanel);
            var resources = new AmaceResources();
            Panels.RegisterPanel(plugin, cupPanelType, "Cup", new Icon(resources.CupPanelIconFile));
        }

        private static void InitializeScrewPanel(RhinoDoc document)
        {
            // Screw Panel
            var screwPanel = ScrewPanel.GetPanel();
            if (screwPanel != null)
            {
                screwPanel.doc = document;
            }
        }

        private static void InitializeCupPanel(RhinoDoc document)
        {
            // Cup Panel
            var cupPanel = CupPanel.GetPanel();
            if (cupPanel != null)
            {
                cupPanel.document = document;
            }
        }

        #endregion
    }
}