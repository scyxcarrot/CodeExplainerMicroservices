using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for ImportRecut.xaml
    /// </summary>
    public partial class ImportRecut : Window
    {
        public ImportRecut()
        {
            InitializeComponent();
        }

        public void SetWrongPartNameList(List<string> wrongPartNames)
        {
            wrongNamingConventionListBox.ItemsSource = wrongPartNames.OrderBy(n => n);
            wrongNamingConventionGrid.Visibility = wrongPartNames.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetNotGoingToBeImportedPartNameList(List<string> notGoingToBeImportedPartNames)
        {
            notImportedListBox.ItemsSource = notGoingToBeImportedPartNames.OrderBy(n => n);
            notImportedGrid.Visibility = notGoingToBeImportedPartNames.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
