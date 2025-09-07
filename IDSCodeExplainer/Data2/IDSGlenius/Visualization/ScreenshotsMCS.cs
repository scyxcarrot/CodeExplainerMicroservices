using IDS.Core.Visualization;
using System;
using System.Drawing;

namespace IDS.Glenius.Visualization
{
    public class ScreenshotsMCS
    {
        private readonly bool _defectIsLeft;
        private readonly CameraViewPresets _cameraViewPresets;

        public ScreenshotsMCS(GleniusImplantDirector director)
        {
            _defectIsLeft = director.defectIsLeft;
            _cameraViewPresets = new CameraViewPresets(director.AnatomyMeasurements, director.Document.Views.ActiveView.ActiveViewport, _defectIsLeft);
        }

        public void SetCameraForView(CameraView cameraView)
        {
            switch (cameraView)
            {
                case CameraView.Superior:
                    _cameraViewPresets.SetCameraToSuperiorView();
                    break;
                case CameraView.Anterior:
                    _cameraViewPresets.SetCameraToAnteriorView();
                    break;
                case CameraView.Anterolateral:
                    _cameraViewPresets.SetCameraToAnteroLateralView();
                    break;
                case CameraView.Lateral:
                    _cameraViewPresets.SetCameraToLateralView();
                    break;
                case CameraView.Posterolateral:
                    _cameraViewPresets.SetCameraToPosteroLateralView();
                    break;
                case CameraView.Posterior:
                    _cameraViewPresets.SetCameraToPosteriorView();
                    break;
                case CameraView.Medial:
                    _cameraViewPresets.SetCameraToMedialView();
                    break;
                case CameraView.Inferior:
                    _cameraViewPresets.SetCameraToInferiorView();
                    break;
            }
        }

        public Bitmap CropBitmap(Bitmap image, CameraView cameraView)
        {
            var bitmapWidth = image.Width;
            var bitmapHeight = image.Height;
            var offsetX = 0;
            const int offsetY = 0;

            switch (cameraView)
            {
                case CameraView.Superior:
                    bitmapWidth = Convert.ToInt32(image.Width * 0.8);
                    if (bitmapWidth < image.Height)
                    {
                        bitmapWidth = image.Height;
                    }
                    if (_defectIsLeft)
                    {
                        offsetX = image.Width - bitmapWidth;
                    }
                    break;
                case CameraView.Anterior:
                    bitmapWidth = Convert.ToInt32(image.Width * 0.8);
                    bitmapHeight = image.Height / 2;
                    if (bitmapWidth < bitmapHeight)
                    {
                        bitmapWidth = bitmapHeight;
                    }
                    if (bitmapHeight < bitmapWidth)
                    {
                        bitmapHeight = bitmapWidth;
                    }
                    if (_defectIsLeft)
                    {
                        offsetX = image.Width - bitmapWidth;
                    }
                    break;
                case CameraView.Anterolateral:
                case CameraView.Posterolateral:
                case CameraView.Lateral:
                    bitmapHeight = image.Height / 2;
                    if (bitmapHeight < image.Width)
                    {
                        bitmapHeight = image.Width;
                    }
                    break;
            }

            if (image.Width < bitmapWidth || image.Height < bitmapHeight ||
                (image.Width == bitmapWidth && image.Height == bitmapHeight))
            {
                return image;
            }

            var bitmap = new Bitmap(bitmapWidth, bitmapHeight);
            for (var x = 0; x < bitmap.Width; ++x)
            {
                for (var y = 0; y < bitmap.Height; ++y)
                {
                    bitmap.SetPixel(x, y, image.GetPixel(x + offsetX, y + offsetY));
                }
            }
            image.Dispose();
            var croppedImage = new Bitmap(bitmap);
            bitmap.Dispose();
            return croppedImage;
        }
    }
}