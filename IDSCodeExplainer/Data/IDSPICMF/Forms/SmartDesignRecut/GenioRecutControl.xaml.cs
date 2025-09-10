using System.Windows;
using System.Windows.Controls;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for GenioRecutControl.xaml
    /// </summary>
    public partial class GenioRecutControl : UserControl, ISelectableControl
    {
        public OnSelectPartDelegate OnSelectPartEventHandler { get; set; }

        public GenioRecutViewModel ViewModel;

        public GenioRecutControl()
        {
            ViewModel = new GenioRecutViewModel();
            DataContext = ViewModel;
            InitializeComponent();

            var row = 0;
            foreach (var part in ViewModel.PartSelections.Values)
            {
                if (!part.IsSeparateContainer)
                {
                    ControlContainer.RowDefinitions.Add(new RowDefinition());

                    var item = new PartSelectionControl
                    {
                        DataContext = part
                    };
                    ControlContainer.Children.Add(item);
                    Grid.SetRow(item, row);
                    item.BtnSelect.Click += (sender, e) => OnSelectPartEventHandler?.Invoke(part);
                    row++;
                }
                else
                {
                    WedgeOperationContainer.RowDefinitions.Add(new RowDefinition());
                    var item = new PartSelectionControl
                    {
                        DataContext = part
                    };
                    WedgeOperationContainer.Children.Add(item);
                    item.BtnSelect.Click += (sender, e) => OnSelectPartEventHandler?.Invoke(part);
                }
            }
        }

        public void CleanUp()
        {
            OnSelectPartEventHandler = null;
            ControlContainer.Children.Clear();
            ViewModel.CleanUp();
        }

        private void WedgeCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel.WedgeOperation)
            {
                WedgeOperationContainer.IsEnabled = true;
            }
            else
            {
                WedgeOperationContainer.IsEnabled = false;
            }
        }
    }
}
