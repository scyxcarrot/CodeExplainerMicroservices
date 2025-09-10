using System;
using System.Drawing;
using System.IO;

namespace IDS.Core.Utilities
{
    public static class ImageUtilities
    {
        /**
         * Convert any object inheriting from System.Drawing.Image
         * (e.g. System.Drawing.Bitmap) to a Jpeg-encoded base64 string.
         */

        public static string ImageToBase64Jpeg(System.Drawing.Image img)
        {
            using (var memstream = new System.IO.MemoryStream())
            {
                // Conver bitmap to JPEG and save the byte representation as Base 64 string
                img.Save(memstream, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] byteArray = memstream.ToArray();
                return Convert.ToBase64String(byteArray);
            }
        }

        public static bool SaveBase64JpegToFile(string imageStringByteArray, string saveFolderPath, string fileNameNoExtension, string extension)
        {
            try
            {
                var bytes = Convert.FromBase64String(imageStringByteArray);
                using (var imageFile = new FileStream($"{saveFolderPath}\\{fileNameNoExtension}.{extension}", FileMode.Create))
                {
                    imageFile.Write(bytes, 0, bytes.Length);
                    imageFile.Flush();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static Image Base64JpegToImage(string jpegBase64)
        {
            using (var memoryStream = new MemoryStream(Convert.FromBase64String(jpegBase64)))
            {
                return Image.FromStream(memoryStream);
            }
        }
    }
}