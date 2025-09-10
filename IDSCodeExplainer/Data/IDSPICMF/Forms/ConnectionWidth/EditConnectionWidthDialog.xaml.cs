using System.Windows;

namespace IDS.PICMF.Forms
{
    public partial class EditConnectionWidthDialog : Window
    {
        public EditConnectionWidthDialog(ConnectionWidthViewModel dataModel)
        {
            InitializeComponent();

            DataContext = dataModel;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
