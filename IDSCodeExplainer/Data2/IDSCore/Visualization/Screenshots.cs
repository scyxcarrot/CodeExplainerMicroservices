using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace IDS.Core.Visualization
{
    public static class Screenshots
    {
        /// <summary>
        /// Generates the image.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="bbox">The bbox.</param>
        /// <param name="crop">if set to <c>true</c> [crop].</param>
        /// <returns></returns>
        public static Bitmap GenerateImage(RhinoDoc doc, int width, int height, BoundingBox bbox, bool crop)
        {
            return GenerateImage(doc, width, height, bbox, crop, "IDS");
        }

        public static Bitmap GenerateImage(RhinoDoc doc, int width, int height, BoundingBox bbox, bool crop, string displayModeName)
        {
            // Set perspective view to parallel projection
            doc.Views.ActiveView.ActiveViewport.ChangeToParallelProjection(true);
            // Store current display mode (in case user changed it)
            var userMode = doc.Views.ActiveView.ActiveViewport.DisplayMode;
            // Set IDS display mode (if necessary)
            if (userMode.EnglishName != displayModeName)
            {
                View.SetViewAndDisplayMode(doc, doc.Views.ActiveView.ActiveViewport.Name, displayModeName);
            }
            // Limit view to predefined bounding box of visible objects (+ margin for text and annotations)
            if (bbox.IsValid)
            {
                doc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(bbox);
            }
            var screenshotSize = new Size(width, height);
            var viewportSize = doc.Views.ActiveView.ActiveViewport.Size;
            doc.Views.ActiveView.ActiveViewport.Size = screenshotSize;
            doc.Views.Redraw();

            var img = CaptureToBitmap(doc, screenshotSize, crop);

            // Set back to user specified display mode (if necessary)
            if (userMode.EnglishName != displayModeName)
            {
                doc.Views.ActiveView.ActiveViewport.DisplayMode = userMode;
            }
            // Refresh
            doc.Views.ActiveView.ActiveViewport.Size = viewportSize;
            doc.Views.Redraw();
            // Return the image
            return img;
        }

        public static string[][] GenerateRotatingImageStrings(RhinoDoc doc, int width, int height, int rotationStepDegrees, int minTiltDegrees, int maxTiltDegrees, int tiltStepDegrees)
        {
            const int minRotationDegrees = 0;
            var maxRotationDegrees = 360 - rotationStepDegrees;

            var tiltIndex = 0;
            var rotationIndex = 0;

            var initialCameraLocation = doc.Views.ActiveView.ActiveViewport.CameraLocation;
            var initialCameraUp = doc.Views.ActiveView.ActiveViewport.CameraUp;
            var initialCameraDirection = doc.Views.ActiveView.ActiveViewport.CameraDirection;
            var cameraTarget = doc.Views.ActiveView.ActiveViewport.CameraTarget;

            var tiltRange = MathUtilities.Range(minTiltDegrees, maxTiltDegrees, tiltStepDegrees).ToArray();
            var rotationRange = MathUtilities.Range(minRotationDegrees, maxRotationDegrees, rotationStepDegrees).ToArray();
            var imageStrings = new string[tiltRange.Length][];

            foreach (var tiltDegrees in tiltRange)
            {
                // Initialize rotation imagestring array
                imageStrings[tiltIndex] = new string[rotationRange.Length];

                // Set tilt transform
                var tiltAxis = Vector3d.CrossProduct(initialCameraUp, initialCameraDirection); // cross product of up vector and camera direction
                var tiltTransform = Transform.Rotation(RhinoMath.ToRadians(tiltDegrees), tiltAxis, cameraTarget);

                foreach (var rotationDegrees in rotationRange)
                {
                    // Tilt camera
                    var rotatedCameraLocation = initialCameraLocation;
                    rotatedCameraLocation.Transform(tiltTransform);
                    var rotatedCameraUp = initialCameraUp;
                    rotatedCameraUp.Transform(tiltTransform);
                    // Rotate around up vector
                    var rotationTransform = Transform.Rotation(RhinoMath.ToRadians(rotationDegrees), rotatedCameraUp, cameraTarget);
                    rotatedCameraLocation.Transform(rotationTransform);
                    // Set the camera in the scene
                    doc.Views.ActiveView.ActiveViewport.SetCameraLocations(cameraTarget, rotatedCameraLocation);
                    doc.Views.ActiveView.ActiveViewport.CameraUp = rotatedCameraUp;
                    doc.Views.Redraw();
                    // Create image string
                    var image = GenerateImage(doc, width, height, BoundingBox.Unset, false);
                    var imageString = GenerateImageString(image);
                    // Add to imagestrings
                    imageStrings[tiltIndex][rotationIndex] = imageString;

                    rotationIndex++;
                }

                // Set indices for next tilt
                rotationIndex = 0;
                tiltIndex++;
            }

            return imageStrings;
        }

        /// <summary>
        /// Adds the overlay.
        /// </summary>
        /// <param name="background">The background.</param>
        /// <param name="overlay">The overlay.</param>
        /// <param name="transparancy">The transparancy.</param>
        public static void AddOverlay(ref Bitmap background, Bitmap inputOverlay, Color transparancy)
        {
            var overlay = inputOverlay;
            overlay.MakeTransparent(transparancy);

            if (background.Size != overlay.Size)
            {
                var overlayResize = new Bitmap(overlay, background.Size);
                overlay.Dispose();
                overlay = new Bitmap(overlayResize);
                overlayResize.Dispose();
            }

            for (var x = 0; x < background.Size.Width; x++)
            {
                for (var y = 0; y < background.Size.Height; y++)
                {
                    //if (!EqualColor(overlay.GetPixel(x, y), transparancy, threshold))
                    if (overlay.GetPixel(x, y).A == 255)
                    {
                        background.SetPixel(x, y, overlay.GetPixel(x, y));
                    }
                }
            }
        }

        /// <summary>
        /// Equals the color.
        /// </summary>
        /// <param name="color1">The color1.</param>
        /// <param name="color2">The color2.</param>
        /// <param name="threshold">The threshold.</param>
        /// <returns></returns>
        public static bool EqualColor(Color color1, Color color2, int threshold = 0)
        {
            return (Math.Abs((int)color1.R - (int)color2.R) < threshold) &&
                    (Math.Abs((int)color1.G - (int)color2.G) < threshold) &&
                    (Math.Abs((int)color1.B - (int)color2.B) < threshold);
        }

        /// <summary>
        /// Convert an image to a base 64 string and dispose if necessary
        /// </summary>
        /// <param name="img">            </param>
        /// <param name="disposeOriginal"></param>
        /// <returns></returns>
        public static string GenerateImageString(Image img, bool disposeOriginal = true)
        {
            var imageString = ImageUtilities.ImageToBase64Jpeg(img);

            if (disposeOriginal)
            {
                img.Dispose();
            }

            return imageString;
        }

        /// <summary>
        /// Trim the whitespace around an image
        /// Source: http://stackoverflow.com/questions/4820212/automatically-trim-a-bitmap-to-minimum-size
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Bitmap TrimBitmap(Bitmap source)
        {
            var whiteR = 255;
            var whiteG = 255;
            var whiteB = 255;
            return TrimBitmap(source, whiteR, whiteG, whiteB);
        }

        public static Bitmap TrimBitmap(Bitmap source, int rValueToTrim, int gValueToTrim, int bValueToTrim)
        {
            Rectangle srcRect;
            BitmapData data = null;
            try
            {
                data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var buffer = new byte[data.Height * data.Stride];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                int xMin = int.MaxValue,
                    xMax = int.MinValue,
                    yMin = int.MaxValue,
                    yMax = int.MinValue;

                var foundPixel = false;

                // Find xMin
                for (var x = 0; x < data.Width; x++)
                {
                    var stop = false;
                    for (var y = 0; y < data.Height; y++)
                    {
                        var isTrimColor = (buffer[y * data.Stride + 4 * x] == rValueToTrim && buffer[y * data.Stride + 4 * x + 1] == gValueToTrim && buffer[y * data.Stride + 4 * x + 2] == bValueToTrim);
                        if (isTrimColor)
                        {
                            continue;
                        }

                        xMin = x;
                        stop = true;
                        foundPixel = true;
                        break;
                    }
                    if (stop)
                    {
                        break;
                    }
                }

                // Image is empty...
                if (!foundPixel)
                {
                    return null;
                }

                // Find yMin
                for (var y = 0; y < data.Height; y++)
                {
                    var stop = false;
                    for (var x = xMin; x < data.Width; x++)
                    {
                        var isTrimColor = (buffer[y * data.Stride + 4 * x] == rValueToTrim && buffer[y * data.Stride + 4 * x + 1] == gValueToTrim && buffer[y * data.Stride + 4 * x + 2] == bValueToTrim);
                        if (isTrimColor)
                        {
                            continue;
                        }

                        yMin = y;
                        stop = true;
                        break;
                    }
                    if (stop)
                    {
                        break;
                    }
                }

                // Find xMax
                for (var x = data.Width - 1; x >= xMin; x--)
                {
                    var stop = false;
                    for (var y = yMin; y < data.Height; y++)
                    {
                        var isTrimColor = (buffer[y * data.Stride + 4 * x] == rValueToTrim && buffer[y * data.Stride + 4 * x + 1] == gValueToTrim && buffer[y * data.Stride + 4 * x + 2] == bValueToTrim);
                        if (isTrimColor)
                        {
                            continue;
                        }
                        xMax = x;
                        stop = true;
                        break;
                    }
                    if (stop)
                    {
                        break;
                    }
                }

                // Find yMax
                for (var y = data.Height - 1; y >= yMin; y--)
                {
                    var stop = false;
                    for (var x = xMin; x <= xMax; x++)
                    {
                        var isTrimColor = (buffer[y * data.Stride + 4 * x] == rValueToTrim && buffer[y * data.Stride + 4 * x + 1] == gValueToTrim && buffer[y * data.Stride + 4 * x + 2] == bValueToTrim);
                        if (isTrimColor)
                        {
                            continue;
                        }

                        yMax = y;
                        stop = true;
                        break;
                    }
                    if (stop)
                    {
                        break;
                    }
                }

                srcRect = Rectangle.FromLTRB(xMin, yMin, xMax, yMax);
            }
            finally
            {
                if (data != null)
                {
                    source.UnlockBits(data);
                }
            }

            var dest = new Bitmap(srcRect.Width, srcRect.Height);
            var destRect = new Rectangle(0, 0, srcRect.Width, srcRect.Height);
            using (var graphics = Graphics.FromImage(dest))
            {
                graphics.DrawImage(source, destRect, srcRect, GraphicsUnit.Pixel);
            }
            return dest;
        }

        public static Bitmap GenerateMeshImage(RhinoDoc doc, Mesh mesh, int width, int height, CameraView view)
        {
            // Hide everything
            Visibility.HideAll(doc);
            // Add difference mesh to document and show
            var differenceMeshGuid = doc.Objects.AddMesh(mesh);
            // Create screenshot
            var screenshot = GenerateImage(doc, width, height, BoundingBox.Unset, true);
            // Remove difference mesh from document
            doc.Objects.Delete(differenceMeshGuid, true);
            // Return screenshot
            return screenshot;
        }

        public static Bitmap CaptureToBitmap(RhinoDoc doc, Size screenshotSize, bool crop)
        {      
            var img = doc.Views.ActiveView.CaptureToBitmap(screenshotSize, false, false, false);
            if (crop)
            {
                Bitmap imgTrim = TrimBitmap(img);
                if (imgTrim != null)
                {
                    img.Dispose();
                    img = new Bitmap(imgTrim);
                    imgTrim.Dispose();
                }
            }

            return img;
        }
    }
}