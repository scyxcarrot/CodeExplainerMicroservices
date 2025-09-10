using IDS.CMF.Constants;
using Rhino;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for SmartDesignRecutDialog.xaml
    /// </summary>
    public partial class SmartDesignRecutDialog : Window
    {
        private bool _canCloseWithoutConfirmationMsg;

        public PartSelectionViewModel PartSelectionViewModel;
        public IRecutViewModel RecutViewModel;

        public SmartDesignRecutDialog()
        {
            _canCloseWithoutConfirmationMsg = false;
            PartSelectionViewModel = null;
            RecutViewModel = null;
            InitializeComponent();
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

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var child in Container.Children)
            {
                if (child is ISelectableControl selectableControl)
                {
                    selectableControl.CleanUp();
                }
            }

            Container.Children.Clear();

            var control = (ContentControl)sender;
            var recutType = control.Content.ToString();

            switch (recutType.ToUpper())
            {
                case SmartDesignOperations.RecutLefort:
                    var lefortControl = new LefortRecutControl();
                    lefortControl.OnSelectPartEventHandler = data => SelectPart(data);
                    Container.Children.Add(lefortControl);
                    RecutViewModel = lefortControl.ViewModel;
                    PreSelectParts();
                    break;
                case SmartDesignOperations.RecutBSSO:
                    var BSSOControl = new BSSORecutControl();
                    BSSOControl.OnSelectPartEventHandler = data => SelectPart(data);
                    Container.Children.Add(BSSOControl);
                    RecutViewModel = BSSOControl.ViewModel;
                    PreSelectParts();
                    break;
                case SmartDesignOperations.RecutGenio:
                    var genioControl = new GenioRecutControl();
                    genioControl.OnSelectPartEventHandler = data => SelectPart(data);
                    Container.Children.Add(genioControl);
                    RecutViewModel = genioControl.ViewModel;
                    PreSelectParts();
                    break;
                case SmartDesignOperations.RecutSplitMax:
                    var splitMaxControl = new SplitMaxRecutControl();
                    splitMaxControl.OnSelectPartEventHandler = data => SelectPart(data);
                    Container.Children.Add(splitMaxControl);
                    RecutViewModel = splitMaxControl.ViewModel;
                    PreSelectParts();
                    break;
                default:
                    RecutViewModel = null;
                    break;
            }            
        }

        private void SelectPart(PartSelectionViewModel data)
        {
            PartSelectionViewModel = data;
            RhinoApp.RunScript("_Select", true);
        }

        private void PreSelectParts()
        {
            RhinoApp.RunScript("_PreSelect", true);
        }
    }
}
