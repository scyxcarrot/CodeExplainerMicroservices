using IDS.CMF;
using System;
using System.Windows;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for Initialization.xaml
    /// </summary>
    public partial class Initialization : Window, IDisposable
    {
        public InitializationViewModel ViewModel { get; }

        public bool IsEnterPressed { get; private set; }
        public bool IsImportXmlPressed { get; private set; }

        public Initialization()
        {
            InitializeComponent();
            ViewModel = new InitializationViewModel();
            this.DataContext = ViewModel;
            IsEnterPressed = false;
            IsImportXmlPressed = false;
        }

        public void SurgeryTypeSelectionIsEnabled(bool isEnabled)
        {
            SurgeryTypeSelectionStackPanel.IsEnabled = isEnabled;
        }

        private void Enter_OnClick(object sender, RoutedEventArgs e)
        {
            IsEnterPressed = true;
            this.Close();
        }

        private void ImportXML_OnClick(object sender, RoutedEventArgs e)
        {           
            IsImportXmlPressed = true;
            this.Close();
        }

        public void Dispose()
        {

        }
    }
}
