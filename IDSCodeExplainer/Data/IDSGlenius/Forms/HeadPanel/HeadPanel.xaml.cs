using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDSCore.Common.WPFControls;
using Rhino.UI;
using System;
using System.Windows;
using System.Windows.Controls;

namespace IDS.Glenius.Forms
{
    /// <summary>
    /// Interaction logic for HeadPanelWPF.xaml
    /// </summary>
    /// [Guid("B4528CE3-7501-4B1C-B163-536F605B4779")]
    [System.Runtime.InteropServices.Guid("B4528CE3-7501-4B1C-B163-536F605B4779")]
    public partial class HeadPanel : UserControl, IDisposable
    {
        private HeadPanelViewModel vm;
        private const string ViewFailureMessage = "IDS Anatomical Info is not set! Please execute Reconstruction first!";

        #region AccessabilityStatics
        public static HeadPanelViewModel GetPanelViewModel()
        {
            var p = Panels.GetPanel(HeadPanelWpfHost.PanelId) as HeadPanelWpfHost;

            if (p != null)
            {
                return (p.FrameworkElement as HeadPanel).GetViewModel();
            }
            else
            {
                return null;
            }
        }

        public static bool SetEnabled(bool isEnabled)
        {
            var p = Panels.GetPanel(HeadPanelWpfHost.PanelId) as HeadPanelWpfHost;

            if(p != null)
            {
                p.Enabled = isEnabled;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void OpenPanel()
        {
            Panels.OpenPanel(HeadPanelWpfHost.PanelId);
        }

        public static void ClosePanel()
        {
            Panels.ClosePanel(HeadPanelWpfHost.PanelId);
        }

        #endregion

        public HeadPanel()
        {
            InitializeComponent();
            vm = new HeadPanelViewModel();
            this.DataContext = vm;
            this.IsEnabledChanged += PanelIsEnabledChanged;
        }

        public HeadPanelViewModel GetViewModel()
        {
            return vm;
        }

        private void PanelIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //hide measurements and conduits when panel is disabled
            if (!(bool)e.NewValue)
            {
                if (vm.Model.IsHeadComponentMeasurementsVisible)
                {
                    vm.Model.IsHeadComponentMeasurementsVisible = false;
                }

                if (vm.Model.IsGlenoidVectorVisible)
                {
                    vm.Model.IsGlenoidVectorVisible = false;
                }

                if (vm.Model.IsFullSphereVisible)
                {
                    vm.Model.IsFullSphereVisible = false;
                }
            }
            else
            {
                vm.Model.ResetTransparencies();
                vm.UpdateAllVisualizations();
            }
        }

        #region Position

        private void PosSupBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetSuperior())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void PosInfBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetInferior())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void PosPosBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetPosterior())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void PosAntBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetAnterior())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void PosLatBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetLateral())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void PosMedBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetMedial())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void PosSupInfTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as NumericalTextBox;
            double value;
            if (textBox.TryGetTextValue(out value) && !vm.SetSuperiorInferior(value))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void PosAntPosTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as NumericalTextBox;
            double value;
            if (textBox.TryGetTextValue(out value) && !vm.SetAnteriorPosterior(value))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void PosMedLatTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as NumericalTextBox;
            double value;
            if (textBox.TryGetTextValue(out value) && !vm.SetMedialLateral(value))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        #endregion

        #region Orientation

        private void OrntAnterRetroTxt_Initialized(object sender, EventArgs e)
        {
            OrntAnterRetroTxt.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void OrntGlenVersion_Initialized(object sender, EventArgs e)
        {
            OrntGlenVersion.GetBindingExpression(Label.ContentProperty).UpdateSource();
        }

        private void OrntSupInfTxt_Initialized(object sender, EventArgs e)
        {
            OrntSupInfTxt.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void OrntGlenInclination_Initialized(object sender, EventArgs e)
        {
            OrntGlenInclination.GetBindingExpression(Label.ContentProperty).UpdateSource();
        }

        private void OrntAnterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetAnteversion())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void OrntRetroBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetRetroversion())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void OrntSupBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetSuperiorInclination())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void OrntInfBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetInferiorInclination())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void OrntAnterRetroTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as NumericalTextBox;
            double value;
            if (textBox.TryGetTextValue(out value) && !vm.SetAnteRetroVersion(value))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void OrntSupInfTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as NumericalTextBox;
            double value;
            if (textBox.TryGetTextValue(out value) && !vm.SetInferiorSuperiorInclination(value))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        #endregion

        #region CameraViews
        private void VisSupView_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetCameraToSuperiorView())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void VisAntView_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetCameraToAnteriorView())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void VisLatView_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetCameraToLateralView())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void VisPosView_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.SetCameraToPosteriorView())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        #endregion

        #region Measurements
        private void ShowHeadCompMeasurements_Checked(object sender, RoutedEventArgs e)
        {
            if (!vm.ShowHideHeadComponentMeasurements(true))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void ShowHeadCompMeasurements_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!vm.ShowHideHeadComponentMeasurements(false))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void ShowGlenoidVector_Checked(object sender, RoutedEventArgs e)
        {
            if (!vm.ShowHideGlenoidVector(true))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }

        private void ShowGlenoidVector_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!vm.ShowHideGlenoidVector(false))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, ViewFailureMessage);
            }
        }
        #endregion

        #region Transparencies
        private void ShowHeadComponent_Checked(object sender, RoutedEventArgs e)
        {
            if (!vm.UpdateHeadComponentVisualization())
            {
                (sender as CheckBox).IsChecked = false;
                e.Handled = true;
            }
        }

        private void ShowHeadComponent_Unchecked(object sender, RoutedEventArgs e)
        {
            vm.UpdateHeadComponentVisualization();
        }

        private void ShowScapulaReamed_Checked(object sender, RoutedEventArgs e)
        {
            if (!vm.UpdateScapulaReamedVisualization())
            {
                (sender as CheckBox).IsChecked = false;
                e.Handled = true;
            }
        }

        private void ShowScapulaReamed_Unchecked(object sender, RoutedEventArgs e)
        {
            vm.UpdateScapulaReamedVisualization();
        }

        private void ShowReconsScapula_Checked(object sender, RoutedEventArgs e)
        {
            if (!vm.UpdateRecostructedScapulaVisualization())
            {
                (sender as CheckBox).IsChecked = false;
                e.Handled = true;
            }
        }

        private void ShowReconsScapula_Unchecked(object sender, RoutedEventArgs e)
        {
            vm.UpdateRecostructedScapulaVisualization();
        }

        private void ShowFullSphere_Checked(object sender, RoutedEventArgs e)
        {
            vm.UpdateFullSphereVisualization();
        }

        private void ShowFullSphere_Unchecked(object sender, RoutedEventArgs e)
        {
            vm.UpdateFullSphereVisualization();
        }

        private void ShowCylinder_Checked(object sender, RoutedEventArgs e)
        {
            if (!vm.UpdateCylinderVisualization())
            {
                (sender as CheckBox).IsChecked = false;
                e.Handled = true;
            }
        }

        private void ShowCylinder_Unchecked(object sender, RoutedEventArgs e)
        {
            vm.UpdateCylinderVisualization();
        }

        private void ShowTaperMantleSafetyZone_Checked(object sender, RoutedEventArgs e)
        {
            if (!vm.UpdateTaperMantleSafetyVisualization())
            {
                (sender as CheckBox).IsChecked = false;
                e.Handled = true;
            }
        }

        private void ShowTaperMantleSafetyZone_Unchecked(object sender, RoutedEventArgs e)
        {
            vm.UpdateTaperMantleSafetyVisualization();
        }

        private void ShowProdRod_Checked(object sender, RoutedEventArgs e)
        {
            if (!vm.UpdateProductionRodVsualization())
            {
                (sender as CheckBox).IsChecked = false;
                e.Handled = true;
            }
        }

        private void ShowProdRod_Unchecked(object sender, RoutedEventArgs e)
        {
            vm.UpdateProductionRodVsualization();
        }

        private void TransHeadComponent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm.UpdateHeadComponentVisualization();
        }

        private void TransScapulaReamed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm.UpdateScapulaReamedVisualization();
        }

        private void TransReconsScapula_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm.UpdateRecostructedScapulaVisualization();
        }

        private void TransFullSphere_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm.UpdateFullSphereVisualization();
        }

        private void TransCylinder_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm.UpdateCylinderVisualization();
        }

        private void TransTaperMantleSafetyZone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm.UpdateTaperMantleSafetyVisualization();
        }

        private void TransProdRod_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vm.UpdateProductionRodVsualization();
        }

        #endregion

        private void HeadTypeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.UpdateHeadType();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                vm.Dispose();
            }
        }
    }

    [System.Runtime.InteropServices.Guid("566264F0-DBF5-4D90-8FD6-4C51D86C2AB8")] 
   public class HeadPanelWpfHost : RhinoWindows.Controls.WpfElementHost 
   {
     public HeadPanelWpfHost() : base(new HeadPanel(), null) 
     {
            this.Enabled = false;
     }

       public static System.Guid PanelId => typeof(HeadPanelWpfHost).GUID;
   } 

}
