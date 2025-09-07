using System.Windows.Controls;

namespace IDS.PICMF.Forms.AutoDeployment
{
    /// <summary>
    /// Interaction logic for ProgressBarTemplate.xaml
    /// </summary>
    public partial class ProgressBarTemplate : UserControl
    {
        public readonly DownloadProgressDataModel ProgressDataModel;

        public ProgressBarTemplate(string progressText)
        {
            InitializeComponent();
            ProgressDataModel = new DownloadProgressDataModel(progressText);
            DataContext = ProgressDataModel;
        }
    }
}
