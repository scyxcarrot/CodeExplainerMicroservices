using System.Linq;
using System.Windows;
using System.Windows.Controls;
using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for TeethBlockCreateLimitingSurfaceColumn.xaml
    /// </summary>
    public partial class TeethBlockCreateLimitingSurfaceColumn : UserControl
    {
        private TeethBlockCreateLimitingSurfaceViewModel _vm = new TeethBlockCreateLimitingSurfaceViewModel();

        public TeethBlockCreateLimitingSurfaceViewModel ViewModel
        {
            get => _vm;
            set => _vm = value;
        }

        public TeethBlockCreateLimitingSurfaceColumn()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void CreateLimitingSurface_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript( $"_-CMFTSGCreateLimitSurfaces {GetCastPartName()}", true);
            ViewModel.CommandExecuted?.DynamicInvoke();
        }

        private void EditLimitingSurface_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript($"_-CMFTSGEditLimitSurfaces {GetCastPartName()}", true);
            ViewModel.CommandExecuted?.DynamicInvoke();
        }

        private void DeleteLimitSurface_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript($"_-CMFTSGDeleteLimitSurfaces {GetCastPartName()}", true);
            ViewModel.CommandExecuted?.DynamicInvoke();
        }

        private string GetCastPartName()
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)RhinoDoc.ActiveDoc.RuntimeSerialNumber);
            var proPlanImportComponent = new ProPlanImportComponent();
            var objManager = new CMFObjectManager(director);

            TeethSupportedGuideUtilities.GetCastPartAvailability(objManager, 
                out var availableParts, out var _, ViewModel.SelectedPartType);

            var partName = proPlanImportComponent.GetPartName(availableParts[0].Block.Name);

            if (availableParts.Count > 1)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"There are 2 {ViewModel.SelectedPartType.ToString()}, using {partName} as default.");
            }

            return partName;
        }
    }
}
