using IDS.CMF.DataModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace IDS.PICMF.Forms
{
    public partial class UpdatePlanning : Window
    {
        private UpdatePlanningViewModel _vm;

        public UpdatePlanning()
        {
            InitializeComponent();

            _vm = new UpdatePlanningViewModel();
            DataContext = _vm;
        }

        public void PopulateTable(List<ImportCheckboxModel> importCheckboxList)
        {
            _vm.DataGridImport = new ObservableCollection<ImportCheckboxModel>(importCheckboxList);
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; 
            Close();
        }

        public List<string> GetSelectedItems()
        {
            return _vm.DataGridImport.Where(i => i.IsImportSelected).Select(i => i.PlanningObjectName).ToList();
        }
    }
}
