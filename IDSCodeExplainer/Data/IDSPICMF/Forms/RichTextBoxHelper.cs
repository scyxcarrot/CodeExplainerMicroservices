using IDS.PICMF.Forms;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;

namespace IDS.PICMF.Forms
{
    public static class RichTextBoxHelper
    {
        private static Image LoadImage(string imageDir)
        {
            var image = CreateBitmapImageFromUri(imageDir);

            var img = new RichTextBoxImage { Source = image };

            return img;
        }

        public static void AddImageBySelection(ref RichTextBox rtb)
        {
            var imageDir = string.Empty;
            using (System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog())
            {
                dlg.Title = "Open Image";
                dlg.Filter = "Image files (*.bmp, *.jpg, *.jpeg, *.jpe, *.jfif, *.png, *.gif) | *.bmp; *.jpg; *.jpeg; *.jpe; *.jfif; *.png; *.gif";

                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    imageDir = dlg.FileName;
                }
            }

            if (imageDir == string.Empty)
            {
                return;
            }

            var img = LoadImage(imageDir);
            img.MaxWidth = 50;
            img.MaxHeight = 50;

            var inliner = new InlineUIContainer(img);
            AddInline(ref rtb, inliner);
        }

        public static void OnImageClick(Image img, ref RichTextBox rtb)
        {
            Bitmap bitmap;
            using (var ms = new MemoryStream())
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)img.Source));
                encoder.Save(ms);

                using (var bmp = new Bitmap(ms))
                {
                    bitmap = new Bitmap(bmp);
                }
            }

            var window = new PictureWindow();
            window.ImageBox.Source = img.Source;
            window.Width = bitmap.Width;
            window.Height = bitmap.Height;
            window.MaxWidth = bitmap.Width;
            window.MaxHeight = bitmap.Height;
            window.MinWidth = 50;
            window.MinHeight = 50;
            window.Topmost = true;
            window.Show();
        }

        public static BitmapImage CreateBitmapImageFromUri(string imageUri)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(imageUri);
            image.EndInit();
            image.Freeze();
            return image;
        }

        public static void HandlePastingImage(ref RichTextBox rtb)
        {
            var img = Clipboard.GetImage();
            if (img == null)
            {
                return;
            }

            var tempPath = Path.GetTempPath() + "IDS_" + Guid.NewGuid() + ".jpg";
            using (var fileStream = new FileStream(tempPath, FileMode.Create))
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(img));
                encoder.Save(fileStream);
            }

            var image = CreateBitmapImageFromUri(tempPath);
            File.Delete(tempPath);

            var inliner = new InlineUIContainer(new RichTextBoxImage() {Source = image, Height = 50, Width = 50});
            AddInline(ref rtb, inliner);
        }

        public static void HandleImagesInitialization(ref RichTextBox rtb)
        {
            var blocks = rtb.Document.Blocks.ToList();
            foreach (var documentBlock in blocks)
            {
                if (!(documentBlock is Paragraph))
                {
                    continue;
                }

                var paragraph = (Paragraph) documentBlock;
                var inlines = paragraph.Inlines.OfType<InlineUIContainer>().ToList();
                foreach (var inline in inlines)
                {
                    if (!(inline.Child is Image))
                    {
                        continue;
                    }

                    var image = (Image) inline.Child;
                    inline.Child = new RichTextBoxImage { Source = image.Source, MaxHeight = 50, MaxWidth = 50 };
                }
            }
        }

        private static void AddInline(ref RichTextBox rtb, Inline inline)
        {
            if (rtb.CaretPosition.Paragraph != null)
            {
                rtb.CaretPosition.Paragraph.Inlines.Add(inline);
            }
            else
            {
                var paragraph = new Paragraph(inline);
                rtb.Document.Blocks.Add(paragraph);
            }
        }
    }
}
