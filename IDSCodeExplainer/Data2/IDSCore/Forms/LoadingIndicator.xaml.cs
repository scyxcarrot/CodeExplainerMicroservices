using System;
using System.Windows;

namespace IDSCore.Forms
{
    /// <summary>
    /// Interaction logic for LoadingIndicator.xaml
    /// </summary>
    public partial class LoadingIndicator : Window, IDisposable
    {
        public LoadingIndicator()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            Close();
        }
    }
}
