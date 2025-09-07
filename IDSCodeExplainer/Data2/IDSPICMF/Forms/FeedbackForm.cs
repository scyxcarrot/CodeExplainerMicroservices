using System.Windows.Forms;

namespace IDS.PICMF.Forms
{
    public partial class FeedbackForm : Form
    {
        public string InitialUrl { get; set; }

        public FeedbackForm()
        {
            InitializeComponent();
        }

        private void FeedbackForm_Shown(object sender, System.EventArgs e)
        {
            webBrowser.Navigate(InitialUrl);
        }
    }
}
