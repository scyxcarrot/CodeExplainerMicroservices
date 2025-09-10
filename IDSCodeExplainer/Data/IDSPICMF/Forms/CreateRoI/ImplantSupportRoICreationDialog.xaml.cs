using Rhino;
using System.ComponentModel;
using System.Windows;

namespace IDS.PICMF.Forms
{
    public partial class ImplantSupportRoICreationDialog : Window
    {
        private bool _canCloseWithoutConfirmationMsg;

        public readonly ImplantSupportRoICreationDataModel RoICreationDataModel;

        public ImplantSupportRoICreationDialog(ImplantSupportRoICreationDataModel roICreationDataModel)
        {
            _canCloseWithoutConfirmationMsg = false;
            InitializeComponent();
            RoICreationDataModel = roICreationDataModel;
            DataContext = RoICreationDataModel;
        }

        private void BtnDrawTransition_Click(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("_DrawTransition", true);
        }

        private void BtnMetal_Click(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("_MetalIntegration", true);
        }

        private void BtnTrimRemovedMetal_Click(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("_TrimRemovedMetal", true);
        }

        private void BtnTeeth_Click(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("_TeethIntegration", true);
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("_Preview", true);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("_OK", true);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (CancelConfirmationFromDialog())
            {
                ForceClose();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // If Rhino still working on the action trigger by this dialog box, it will not able to close the dialog box
            if (!IsEnabled)
            {
                e.Cancel = true;
                return;
            }

            if (_canCloseWithoutConfirmationMsg)
            {
                return;
            }
            
            e.Cancel = !CancelConfirmationFromDialog();
        }

        public void ForceClose()
        {
            _canCloseWithoutConfirmationMsg = true;
            IsEnabled = true;
            Close();
        }

        private bool CancelConfirmationFromDialog()
        {
            if (!CancelConfirmation())
            {
                return false;
            }
            
            RhinoApp.RunScript("_Cancel", true);
            return true;
        }

        public bool CancelConfirmation()
        {
            if (_canCloseWithoutConfirmationMsg)
            {
                return true;
            }

            var result = MessageBox.Show(
                "Are you sure want to cancel the operation?",
                "Cancel Operation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return _canCloseWithoutConfirmationMsg = (result == MessageBoxResult.Yes);
        }
    }
}
