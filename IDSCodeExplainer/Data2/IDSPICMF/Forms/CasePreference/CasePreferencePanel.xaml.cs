using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.Enumerators;
using IDS.CMF.Utilities;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.UI;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Visibility = System.Windows.Visibility;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for CasePreferencePanel.xaml
    /// </summary>
    [System.Runtime.InteropServices.Guid("71975693-C66B-44D6-B4E6-F19CD4283467")]
    public partial class CasePreferencePanel : UserControl, IDisposable
    {
        #region AccessabilityStatics
        public static CasePreferencePanelViewModel GetPanelViewModel()
        {
            var p = Panels.GetPanel<CasePreferencePanelWpfHost>(RhinoDoc.ActiveDoc);
            return (p?.FrameworkElement as CasePreferencePanel)?.GetViewModel();
        }

        public static CasePreferencePanel GetView()
        {
            var p = Panels.GetPanel<CasePreferencePanelWpfHost>(RhinoDoc.ActiveDoc);

            return p?.FrameworkElement as CasePreferencePanel;
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

            var p = Panels.GetPanel<CasePreferencePanelWpfHost>(RhinoDoc.ActiveDoc);

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

        public void SetAddImplantVisibility(Visibility visibility)
        {
            var view = GetView();
            view.btnAddImplant.Visibility = visibility;
        }

        public void SetAddGuideVisibility(Visibility visibility)
        {
            var view = GetView();
            view.btnAddGuide.Visibility = visibility;
        }

        public static void OpenPanel()
        {
            var panelId = CasePreferencePanelWpfHost.PanelId;
            var panelVisible = Panels.IsPanelVisible(panelId);

            if (panelVisible)
            {
                Panels.OpenPanel(panelId, true);
                Panels.OnShowPanel(panelId, RhinoDoc.ActiveDoc.RuntimeSerialNumber, true);

                var casePrefViewModel = GetPanelViewModel();
                if (!casePrefViewModel.IsInitialized())
                {
                    InitializePanel(ref casePrefViewModel);
                }
            }
            else
            {
                Panels.OpenPanel(panelId, true);
                Panels.OnShowPanel(panelId, RhinoDoc.ActiveDoc.RuntimeSerialNumber, true);

                var casePrefViewModel = GetPanelViewModel();
                InitializePanel(ref casePrefViewModel);
            }
        }

        public static void InitializePanel(ref CasePreferencePanelViewModel casePrefViewModel)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)RhinoDoc.ActiveDoc.RuntimeSerialNumber);

            casePrefViewModel.ClearPanelUI();
            casePrefViewModel.InitializeDirector(director);
            var cps = director.CasePrefManager.CasePreferences.Select(cp => (ImplantPreferenceModel)cp).ToList();
            casePrefViewModel.InitializeCasePreferencesUI(cps);
            var gps = director.CasePrefManager.GuidePreferences.Select(cp => (GuidePreferenceModel)cp).ToList();
            casePrefViewModel.InitializeGuidePreferencesUI(gps);
            casePrefViewModel.InitializeInformationOnSurgeryUI();
            casePrefViewModel.InvalidateInformationOnSurgeryData();

            InvalidatePanelPhasePreset();
        }

        public static void InvalidatePanelPhasePreset()
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)RhinoDoc.ActiveDoc.RuntimeSerialNumber);
            var designPhase = GeneralUtilities.GetDesignPhase(director);

            switch (designPhase)
            {
                case DesignPhase.Draft:
                    GetView().SetToDraftPreset();
                    break;
                case DesignPhase.Planning:
                    GetView().SetToPlanningPhasePreset();
                    break;
                case DesignPhase.PlanningQC:
                    GetView().SetToPlanningQcPhasePreset();
                    break;
                case DesignPhase.Implant:
                    GetView().SetToImplantPhasePreset();
                    break;
                case DesignPhase.Guide:
                case DesignPhase.TeethBlock:
                    GetView().SetToGuidePhasePreset();
                    break;
                case DesignPhase.MetalQC:
                    GetView().SetToMetalQcPhasePreset();
                    break;
            }
        }

        public static void ClosePanel()
        {
            Panels.ClosePanel(CasePreferencePanelWpfHost.PanelId);
        }

        #endregion

        private CasePreferencePanelViewModel _vm = new CasePreferencePanelViewModel();

        public CasePreferencePanel()
        {
            InitializeComponent();
            this.DataContext = _vm;


            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                SetEnabled(true);
            }));
        }

        public CasePreferencePanelViewModel GetViewModel()
        {
            return _vm;
        }

        public void InvalidateUI()
        {
            _vm.InvalidateUI();
        }

        private void BtnAddImplant_OnClick(object sender, RoutedEventArgs e)
        {
            if (IDSPluginHelper.HandleRhinoIsInCommand() || !_vm.IsInitialized())
            {
                return;
            }

            Msai.TrackOpsEvent("Add New Implant", "CMF");

            _vm.AddNewImplant();//TODO [AH] add implant cause buttons to add implant/guide dissapears, check it out
            lvCasePreferencePanel.SelectedIndex = lvCasePreferencePanel.Items.Count - 1;
            lvCasePreferencePanel.ScrollIntoView(lvCasePreferencePanel.SelectedItem);
        }

        private void BtnAddGuide_OnClick(object sender, RoutedEventArgs e)
        {
            if (IDSPluginHelper.HandleRhinoIsInCommand() || !_vm.IsInitialized())
            {
                return;
            }

            Msai.TrackOpsEvent("Add New Guide", "CMF");

            _vm.AddNewGuide();
            lvGuidePreferencePanel.SelectedIndex = lvGuidePreferencePanel.Items.Count - 1;
            lvGuidePreferencePanel.ScrollIntoView(lvGuidePreferencePanel.SelectedItem);
        }

        //Remember to add this in ImplantPreferenceControl for load .3dm file
        public void SetToDraftPreset()
        {
            _vm.CurrPhaseString = $"Current Phase - Draft";
            SetToCommonPreset();
            _vm.GetAllCasePreferenceControls().ForEach(x => x.SetToDraftPhasePreset());
            _vm.GetAllGuidePreferenceControls().ForEach(x => x.SetToDraftPhasePreset());
        }

        public void SetToPlanningPhasePreset()
        {
            _vm.CurrPhaseString = $"Current Phase - Planning";

            SetEnabled(true);

            _vm.GetAllCasePreferenceControls().ForEach(x => x.SetToPlanningPhasePreset());
            _vm.GetAllGuidePreferenceControls().ForEach(x => x.SetToPlanningPhasePreset());
            SetAddImplantVisibility(Visibility.Visible);
            SetAddGuideVisibility(Visibility.Visible);
            _vm.CasePreferenceFieldsEnabled(true);
            _vm.GuidePreferenceFieldsEnabled(true);
            _vm.SurgeryInfoFieldsEnabled(true);
        }

        public void SetToPlanningQcPhasePreset()
        {
            _vm.CurrPhaseString = $"Current Phase - Planning QC";

            SetToCommonPreset();

            _vm.GetAllCasePreferenceControls().ForEach(x => x.SetToPlanningQcPhasePreset());
            _vm.GetAllGuidePreferenceControls().ForEach(x => x.SetToPlanningQcPhasePreset());
        }

        public void SetToImplantPhasePreset()
        {
            _vm.CurrPhaseString = $"Current Phase - Implant";

            SetToCommonPreset();

            _vm.GetAllCasePreferenceControls().ForEach(x => x.SetToImplantPhasePreset());
            _vm.GetAllGuidePreferenceControls().ForEach(x => x.SetToImplantPhasePreset());
        }

        public void SetToGuidePhasePreset()
        {
            _vm.CurrPhaseString = $"Current Phase - Guide";

            SetToCommonPreset();

            _vm.GetAllCasePreferenceControls().ForEach(x => x.SetToGuidePhasePreset());
            _vm.GetAllGuidePreferenceControls().ForEach(x => x.SetToGuidePhasePreset());
        }

        public void SetToMetalQcPhasePreset()
        {
            _vm.CurrPhaseString = $"Current Phase - Metal QC";

            SetToCommonPreset();

            _vm.GetAllCasePreferenceControls().ForEach(x => x.SetToMetalQcPhasePreset());
            _vm.GetAllGuidePreferenceControls().ForEach(x => x.SetToMetalQcPhasePreset());
        }

        private void SetToCommonPreset()
        {
            SetEnabled(true);

            SetAddImplantVisibility(Visibility.Collapsed);
            SetAddGuideVisibility(Visibility.Collapsed);
            _vm.CasePreferenceFieldsEnabled(false);
            _vm.GuidePreferenceFieldsEnabled(false);
            _vm.SurgeryInfoFieldsEnabled(false);
        }

        public void Dispose()
        {
            _vm.Dispose();
        }

        private void LvCasePreferencePanel_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            foreach (var cp in _vm.GetAllCasePreferenceControls())
            {
                if (cp.ActiveSlider != null)
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void LvSurgeryInfoPanel_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_vm.InfoOnSurgeryControl.ActiveSlider != null)
            {
                e.Handled = true;
            }
        }

        private void LvGuidePreferencePanel_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            foreach (var cp in _vm.GetAllGuidePreferenceControls())
            {
                if (cp.ActiveSlider != null)
                {
                    e.Handled = true;
                    return;
                }
            }
        }
    }

    [System.Runtime.InteropServices.Guid("CB648948-DA39-41AA-94DB-0E9A805BBA9A")]
    public class CasePreferencePanelWpfHost : RhinoWindows.Controls.WpfElementHost
    {
        public CasePreferencePanelWpfHost() : base(new CasePreferencePanel(), null)
        {
            this.Enabled = false;
        }

        public static System.Guid PanelId => typeof(CasePreferencePanelWpfHost).GUID;
    }
}
