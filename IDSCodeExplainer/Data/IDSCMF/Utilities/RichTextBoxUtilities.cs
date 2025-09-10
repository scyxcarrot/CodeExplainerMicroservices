using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace IDS.CMF.Utilities
{
    public static class RichTextBoxUtilities
    {
        public static string ConvertRichTextBoxToHtmlString(RichTextBox rtb)
        {
            var blocks = rtb.Document.Blocks;
            return ConvertBlockCollectionToHtmlString(blocks);
        }

        private static string ConvertBlockCollectionToHtmlString(BlockCollection blocks)
        {
            var htmlString = new StringBuilder();

            foreach (var documentBlock in blocks)
            {
                htmlString.Append(ConvertBlockToHtmlString(documentBlock));
            }

            return htmlString.ToString();
        }

        private static string ConvertBlockToHtmlString(Block block)
        {
            var htmlString = new StringBuilder();

            if (block is Paragraph paragraph)
            {
                htmlString.Append(ConvertParagraphToHtmlString(paragraph));
            }
            else if (block is List list)
            {
                htmlString.Append(list.MarkerStyle == TextMarkerStyle.Decimal ? "<ol>" : "<ul>");
                
                foreach (var listitem in list.ListItems)
                {
                    htmlString.Append("<li>" + ConvertBlockCollectionToHtmlString(listitem.Blocks) + "</li>");
                }

                htmlString.Append(list.MarkerStyle == TextMarkerStyle.Decimal ? "</ol>" : "</ul>");
            }

            return htmlString.ToString();
        }

        private static string ConvertParagraphToHtmlString(Paragraph paragraph)
        {
            var htmlString = new StringBuilder();

            var inlines = paragraph.Inlines;
            foreach (var inline in inlines)
            {
                if (inline is InlineUIContainer uiContainer)
                {
                    if (!(uiContainer?.Child is Image image))
                    {
                        continue;
                    }

                    htmlString.Append(ConvertImageToHtmlString(image));
                }
                else if (inline is Run run)
                {
                    htmlString.Append(run.Text);
                }
            }

            htmlString.Append("<br />");

            return htmlString.ToString();
        }

        private static string ConvertImageToHtmlString(Image image)
        {
            string htmlString;

            using (var ms = new MemoryStream())
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)image.Source));
                encoder.Save(ms);
                var array = ms.ToArray();

                var imageString = Convert.ToBase64String(array);

                htmlString = "<a href=\"javascript:popitup('data:image/jpeg;base64," + imageString + "','')\">" +
                           "<img src=\"data:image/jpeg;base64," + imageString +
                           "\" style=\"width:50px;height:50px\"/>" +
                           "</a>";
            }

            return htmlString;
        }
    }
}
