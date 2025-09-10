using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System;
using System.Windows.Forms;

namespace IDS.Core.Utilities
{
    public static class FileUtilities
    {
        public static string GetFileDir(string title, string filter, string errorMessage)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = title,
                Filter = filter,
                InitialDirectory = Environment.SpecialFolder.Desktop.ToString()
            };
            var rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                if (errorMessage != string.Empty)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, errorMessage);
                }

                return string.Empty;
            }

            return dialog.FileName;
        }
    }
}
