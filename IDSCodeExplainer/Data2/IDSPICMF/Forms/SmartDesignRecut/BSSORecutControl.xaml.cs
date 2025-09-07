﻿using System.Windows;
 using System.Windows.Controls;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for BSSORecutControl.xaml
    /// </summary>
    public partial class BSSORecutControl : UserControl, ISelectableControl
    {
        public OnSelectPartDelegate OnSelectPartEventHandler { get; set; }

        public BSSORecutViewModel ViewModel;

        public BSSORecutControl()
        {
            ViewModel = new BSSORecutViewModel();
            DataContext = ViewModel;
            InitializeComponent();

            // Wedge operation is checked by default, so we should disable AnteriorCheckBox at the start
            AnteriorCheckBox.IsEnabled = false;

            var mainColumnRow = 0;
            var separateColumnRow = 0;
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
                    Grid.SetRow(item, mainColumnRow);
                    item.BtnSelect.Click += (sender, e) => OnSelectPartEventHandler?.Invoke(part);
                    mainColumnRow++;
                }
                else
                {
                    WedgeOperationContainer.RowDefinitions.Add(new RowDefinition());
                    var item = new PartSelectionControl
                    {
                        DataContext = part
                    };
                    WedgeOperationContainer.Children.Add(item);
                    Grid.SetRow(item, separateColumnRow);
                    item.BtnSelect.Click += (sender, e) => OnSelectPartEventHandler?.Invoke(part);
                    separateColumnRow++;
                }
            }
        }

        public void CleanUp()
        {
            OnSelectPartEventHandler = null;
            ControlContainer.Children.Clear();
            WedgeOperationContainer.Children.Clear();
            ViewModel.CleanUp();
        }

        private void WedgeCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel.WedgeOperation)
            {
                AnteriorCheckBox.IsEnabled = false;
                WedgeOperationContainer.IsEnabled = true;
            }
            else
            {
                AnteriorCheckBox.IsEnabled = true;
                WedgeOperationContainer.IsEnabled = false;
            }
        }
    }
}
