using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace IDS.CMF.Utilities
{
    public static class ByteUtilities
    {
        public static byte[] ConvertRichTextBoxToBytes(RichTextBox rtb)
        {
            var content = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);

            byte[] data;
            using (var ms = new MemoryStream())
            {
                content.Save(ms, DataFormats.XamlPackage, true);
                data = ms.ToArray();
            }

            return data;
        }

        public static RichTextBox ConvertBytesToRichTextBox(byte[] bytes)
        {
            var richTextBox = new RichTextBox();

            if (bytes == null || bytes.Length == 0)
            {
                return richTextBox;
            }

            var content = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);

            using (var ms = new MemoryStream(bytes))
            {
                content.Load(ms, DataFormats.XamlPackage);
            }

            return richTextBox;
        }
    }
}
