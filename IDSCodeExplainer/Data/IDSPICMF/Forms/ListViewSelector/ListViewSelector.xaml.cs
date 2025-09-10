using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Rhino;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for ListViewSelector.xaml (Intended for internal use only)
    /// </summary>

    public partial class ListViewSelector : Window
    {
        public string SelectedValue { get; set; }
        public ObservableCollection<string> ListViewItems { get; set; }
        
        public ListViewSelector(List<string> listItems, string title)
        {
            InitializeComponent();
            DataContext = this;
            this.IsEnabled = true;
            Window.Title = title;
            
            ListViewItems = new ObservableCollection<string>(listItems);
        }

        protected void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView)
            {
                SelectedValue = listView.SelectedValue.ToString();
            }

            RhinoApp.RunScript("_Selected", true);
        }

        private void HandleWindowClosing(object sender, CancelEventArgs e)
        {
            RhinoApp.RunScript("_Cancel", true);
        }
    }
}
