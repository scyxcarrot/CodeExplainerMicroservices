using System.Windows;
using System.Windows.Controls;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for TeethBlockImportCastColumn.xaml
    /// </summary>
    public partial class TeethBlockImportCastColumn : UserControl
    {
        private TeethBlockImportCastViewModel _vm = new TeethBlockImportCastViewModel();
        public TeethBlockImportCastViewModel ViewModel {
            get => _vm;
            set => _vm = value;
        }

        public TeethBlockImportCastColumn()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void ImportCast_OnClick(object sender, RoutedEventArgs e)
        {
            Rhino.RhinoApp.RunScript("_-CMFTSGImportCast _Enter", true);
            ViewModel.CommandExecuted?.DynamicInvoke();
        }
    }
}
