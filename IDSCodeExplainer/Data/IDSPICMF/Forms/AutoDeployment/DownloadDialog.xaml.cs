using System.Windows;

namespace IDS.PICMF.Forms.AutoDeployment
{
    /// <summary>
    /// Interaction logic for DownloadDialog.xaml
    /// </summary>
    public partial class DownloadDialog : Window
    {
        public ProgressBarTemplate PluginProgressBar;
        public ProgressBarTemplate PbaProgressBar;
        public ProgressBarTemplate PythonProgressBar;

        public DownloadDialog(string version, bool isUpdatePlugin, bool isUpdatePba, bool isUpdatePython)
        {
            InitializeComponent();
            if (isUpdatePlugin)
            {
                PluginProgressBar = new ProgressBarTemplate($"Downloading IDSCMF v{version}");
                PluginPanel.Visibility = Visibility.Visible;
                PluginPanel.Children.Add(PluginProgressBar);
            }

            if (isUpdatePba)
            {
                PbaProgressBar = new ProgressBarTemplate("Downloading newest SmartDesign");
                PbaPanel.Visibility = Visibility.Visible;
                PbaPanel.Children.Add(PbaProgressBar);
            }

            if (isUpdatePython)
            { 
                PythonProgressBar = new ProgressBarTemplate("Downloading newest PBA Python");
                PythonPanel.Visibility = Visibility.Visible;
                PythonPanel.Children.Add(PythonProgressBar);
            }

            DataContext = this;
        }
    }
}
