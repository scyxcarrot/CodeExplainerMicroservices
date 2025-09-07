using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.Relations;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.UI;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UserControl = System.Windows.Controls.UserControl;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for TeethBlockWizardPanel.xaml
    /// </summary>
    [System.Runtime.InteropServices.Guid("CD58D37B-C772-44F6-8527-5C0CEFE7B628")]
    public partial class TeethBlockWizardPanel : UserControl
    {
        private TeethBlockWizardPanelViewModel _vm = new TeethBlockWizardPanelViewModel();
        public TeethBlockWizardPanel()
        {
            InitializeComponent();
            DataContext = _vm;

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                SetEnabled(true);
                InitializeRadioButtons();
                HookRadioButtonEvents();
            }));

            _vm.PanelTitle = "Teeth Block Design";
        }

        public static TeethBlockWizardPanelViewModel GetPanelViewModel()
        {
            var p = Panels.GetPanel<TeethBlockWizardPanelWpfHost>(RhinoDoc.ActiveDoc);
            return (p?.FrameworkElement as TeethBlockWizardPanel)?.GetViewModel();
        }

        public static TeethBlockWizardPanel GetView()
        {
            var p = Panels.GetPanel<TeethBlockWizardPanelWpfHost>(RhinoDoc.ActiveDoc);

            return p?.FrameworkElement as TeethBlockWizardPanel;
        }

        public static bool SetEnabled(bool isEnabled)
        {
            // after rhino crash, for some reason RhinoDoc.ActiveDoc is null
            // if this happens, Panels.GetPanel(RhinoDoc.Activedoc) will return an error during rhino startup
            // this prevents it
            if (RhinoDoc.ActiveDoc == null)
            {
                return false;
            }

            var p = Panels.GetPanel<TeethBlockWizardPanelWpfHost>(RhinoDoc.ActiveDoc);

            if (p != null)
            {
                p.Enabled = isEnabled;
                return true;
            }
            else
            {
                return false;
            }
        }

        public TeethBlockWizardPanelViewModel GetViewModel()
        {
            return _vm;
        }

        public static void OpenPanel()
        {
            var panelId = TeethBlockWizardPanelWpfHost.PanelId;
            Panels.OpenPanel(panelId, true);
            Panels.OnShowPanel(panelId, RhinoDoc.ActiveDoc.RuntimeSerialNumber, true);

            var teethBlockPanelViewModel = GetPanelViewModel(); 
            InitializePanel(ref teethBlockPanelViewModel);
        }

        public static void ClosePanel()
        {
            var panelId = TeethBlockWizardPanelWpfHost.PanelId;
            var panelVisible = Panels.IsPanelVisible(panelId);

            if (panelVisible)
            {
                var teethBlockPanelViewModel = GetPanelViewModel();
                teethBlockPanelViewModel.DisposeEvents();
                Panels.ClosePanel(panelId);
            }
        }

        public static void InitializePanel(ref TeethBlockWizardPanelViewModel teethBlockPanelViewModel)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)RhinoDoc.ActiveDoc.RuntimeSerialNumber);
            var view = GetView();

            teethBlockPanelViewModel.ClearPanelUI();
            teethBlockPanelViewModel.InitializeDirector(director);
            teethBlockPanelViewModel.InitializePanelUI();
            view?.InitializeRadioButtons();
        }

        public void InitializeRadioButtons()
        {
            MaxillaRadioButton.IsChecked = false;
            MandibleRadioButton.IsChecked = true;
        }

        private void HookRadioButtonEvents()
        {
            MaxillaRadioButton.Checked += RadioButton_Checked;
            MandibleRadioButton.Checked += RadioButton_Checked;
        }

        private void BtnGenerate_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript($"_-CMFTSGCreateTeethBlock TeethType {(_vm.SelectedPartType == ProPlanImportPartType.MaxillaCast ? TeethLayer.MaxillaTeeth : TeethLayer.MandibleTeeth)} GuideCaseNumber", true);
            _vm.CommandExecuted?.DynamicInvoke();
        }

        private void BtnExport_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript("_-CMFTSGExportParts", true);
        }

        private void BtnAnalyseThickness_OnClick(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("_-CMFTSGTeethBlockThicknessAnalysis GuideCaseNumber", true);
        }

        private void BtnAnalyseImpression_OnClick(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("_-CMFTSGTeethImpressionDepthAnalysis _Enter", true);
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)RhinoDoc.ActiveDoc.RuntimeSerialNumber);

            var targetPhase = DesignPhase.Guide;
            var success = PhaseChanger.ChangePhase(director, targetPhase);
            if (!success)
            {
                return;
            }

            ClosePanel();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var selectedRadioButton = sender as RadioButton;
            if (selectedRadioButton != null)
            {
                string selectedType = selectedRadioButton.Content.ToString();

                if (selectedType == TeethLayer.MaxillaTeeth)
                {
                    _vm.SelectedPartType = ProPlanImportPartType.MaxillaCast;
                }
                else if (selectedType == TeethLayer.MandibleTeeth)
                {
                    _vm.SelectedPartType = ProPlanImportPartType.MandibleCast;
                }
            }
        }
    }

    [System.Runtime.InteropServices.Guid("CB36D186-2FC6-477A-A258-F5077533E266")]
    public class TeethBlockWizardPanelWpfHost : RhinoWindows.Controls.WpfElementHost
    {
        public TeethBlockWizardPanelWpfHost() : base(new TeethBlockWizardPanel(), null)
        {
            this.Enabled = true;
        }

        public static System.Guid PanelId => typeof(TeethBlockWizardPanelWpfHost).GUID;
    }
}
