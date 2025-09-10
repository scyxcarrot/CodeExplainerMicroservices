using System.Windows.Controls;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for SplitMaxRecutControl.xaml
    /// </summary>
    public partial class SplitMaxRecutControl : UserControl, ISelectableControl
    {
        public OnSelectPartDelegate OnSelectPartEventHandler { get; set; }

        public SplitMaxRecutViewModel ViewModel;

        public SplitMaxRecutControl()
        {
            ViewModel = new SplitMaxRecutViewModel();
            DataContext = ViewModel;
            InitializeComponent();

            var row = 0;
            foreach (var part in ViewModel.PartSelections.Values)
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
        }

        public void CleanUp()
        {
            OnSelectPartEventHandler = null;
            ControlContainer.Children.Clear();
            ViewModel.CleanUp();
        }
    }
}
