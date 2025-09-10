using System.Windows;
using System.Windows.Controls;

namespace IDS.CMF.Forms
{
    public class CancellableComboBox : ComboBox
    {
        public string MessageText
        {
            get { return (string)GetValue(MessageTextProperty); }
            set { SetValue(MessageTextProperty, value); }
        }

        public static readonly DependencyProperty MessageTextProperty =
        DependencyProperty.Register(
            "MessageText",
            typeof(string),
            typeof(CancellableComboBox),
            new PropertyMetadata(null));

        public string MessageTitle
        {
            get { return (string)GetValue(MessageTitleProperty); }
            set { SetValue(MessageTitleProperty, value); }
        }

        public static readonly DependencyProperty MessageTitleProperty =
        DependencyProperty.Register(
            "MessageTitle",
            typeof(string),
            typeof(CancellableComboBox),
            new PropertyMetadata(null));

        public bool IsCancellable
        {
            get { return (bool)GetValue(IsCancellableProperty); }
            set { SetValue(IsCancellableProperty, value); }
        }

        public static readonly DependencyProperty IsCancellableProperty =
        DependencyProperty.Register(
            "IsCancellable",
            typeof(bool),
            typeof(CancellableComboBox),
            new PropertyMetadata(null));

        private bool handleSelection = true;

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (handleSelection)
            {
                if (IsCancellable)
                {
                    var dialogResult = MessageBox.Show(MessageText, MessageTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (dialogResult == MessageBoxResult.No)
                    {
                        handleSelection = false;
                        SelectedItem = e.RemovedItems[0];
                        handleSelection = true;
                        e.Handled = true;
                        return;
                    }
                }

                base.OnSelectionChanged(e);
            }
        }
    }
}