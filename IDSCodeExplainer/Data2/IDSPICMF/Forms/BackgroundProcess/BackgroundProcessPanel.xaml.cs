using System.Windows.Controls;
using System.Windows.Input;

namespace IDS.PICMF.Forms.BackgroundProcess
{
    /// <summary>
    /// Interaction logic for BackgroundProcessPanel.xaml
    /// </summary>
    public partial class BackgroundProcessPanel : UserControl
    {
        public BackgroundProcessPanelViewModel ViewModel { get; }

        public BackgroundProcessPanel()
        {
            InitializeComponent();
            ViewModel = new BackgroundProcessPanelViewModel();
            DataContext = ViewModel;
        }

        private void CompletedTaskPanel_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.ClearCompletedTasks();
        }
    }
}
