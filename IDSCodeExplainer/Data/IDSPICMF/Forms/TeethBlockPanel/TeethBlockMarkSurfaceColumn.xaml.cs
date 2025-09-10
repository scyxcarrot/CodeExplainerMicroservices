using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for TeethBlockMarkSurfaceColumn.xaml
    /// </summary>
    [Guid("582E71A8-78B1-4CFE-98DC-DE996FAA4EFA")]
    public partial class TeethBlockMarkSurfaceColumn : UserControl
    {
        private TeethBlockMarkSurfaceViewModel _vm = new TeethBlockMarkSurfaceViewModel();

        public TeethBlockMarkSurfaceViewModel ViewModel
        {
            get => _vm;
            set => _vm = value;
        }

        public TeethBlockMarkSurfaceColumn()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void DrawBaseRegion_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript($"_-CMFTSGMarkTeethBaseRegion TeethType " +
                                     $"{(ViewModel.SelectedPartType == ProPlanImportPartType.MaxillaCast ? TeethLayer.MaxillaTeeth : TeethLayer.MandibleTeeth)} GuideCaseNumber", true);
            ViewModel.CommandExecuted?.DynamicInvoke();
        }

        private void EditBaseRegion_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript($"_-CMFTSGEditTeethBaseRegion TeethType " +
                                     $"{(ViewModel.SelectedPartType == ProPlanImportPartType.MaxillaCast ? TeethLayer.MaxillaTeeth : TeethLayer.MandibleTeeth)} GuideCaseNumber", true);
            ViewModel.CommandExecuted?.DynamicInvoke();
        }

        private void DrawBracketRegion_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript($"_-CMFTSGMarkBracketRegion TeethType " +
                                     $"{(ViewModel.SelectedPartType == ProPlanImportPartType.MaxillaCast ? TeethLayer.MaxillaTeeth : TeethLayer.MandibleTeeth)}", true);
            ViewModel.CommandExecuted?.DynamicInvoke();
        }

        private void EditBracketRegion_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript($"_-CMFTSGEditBracketRegion TeethType " +
                                     $"{(ViewModel.SelectedPartType == ProPlanImportPartType.MaxillaCast ? TeethLayer.MaxillaTeeth : TeethLayer.MandibleTeeth)}", true);
            ViewModel.CommandExecuted?.DynamicInvoke();
        }

        private void DrawReinforcementRegion_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript($"_-CMFTSGMarkReinforcementRegion TeethType " +
                                     $"{(ViewModel.SelectedPartType == ProPlanImportPartType.MaxillaCast ? TeethLayer.MaxillaTeeth : TeethLayer.MandibleTeeth)}", true);
            ViewModel.CommandExecuted?.DynamicInvoke();
        }

        private void EditReinforcementRegion_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript($"_-CMFTSGEditReinforcementRegion TeethType " +
                                     $"{(ViewModel.SelectedPartType == ProPlanImportPartType.MaxillaCast ? TeethLayer.MaxillaTeeth : TeethLayer.MandibleTeeth)}", true);
            ViewModel.CommandExecuted?.DynamicInvoke();
        }

        private void DeleteSurface_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript($"_-CMFTSGDeleteRegion TeethType " +
                                     $"{(ViewModel.SelectedPartType == ProPlanImportPartType.MaxillaCast ? TeethLayer.MaxillaTeeth : TeethLayer.MandibleTeeth)}", true);
            ViewModel.CommandExecuted?.DynamicInvoke();
        }
    }
}
