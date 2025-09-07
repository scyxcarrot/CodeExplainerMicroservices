using System.Collections.Generic;
using System.Windows;

namespace IDS.PICMF.Forms
{
    public partial class EnlightReferenceObjectPrompt : Window
    {
        public EnlightReferenceObjectPrompt()
        {
            InitializeComponent();
        }

        public void SetPartNames(List<string> partNames)
        {
            partNames.Sort();
            DataContext = partNames;
        }

        private void BtnAbort_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
