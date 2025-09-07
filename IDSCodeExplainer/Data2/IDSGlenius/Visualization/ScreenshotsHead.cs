using IDS.Core.Visualization;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.Glenius.Visualization
{
    public class ScreenshotsHead
    {
        private readonly AnatomicalMeasurements _anatomicalMeasurements;
        private readonly QcDocHeadInformationVisualizer _visualizer;
        private readonly RhinoDoc _doc;
        private readonly ScreenshotsMCS _screenshotsMcs;

        public ScreenshotsHead(GleniusImplantDirector director)
        {
            _doc = director.Document;
            var objectManager = new GleniusObjectManager(director);
            _anatomicalMeasurements = director.AnatomyMeasurements;
            var headAlignment = new HeadAlignment(_anatomicalMeasurements, objectManager, director.Document, director.defectIsLeft);
            _visualizer = new QcDocHeadInformationVisualizer(director.AnatomyMeasurements, headAlignment, director.PreopCor);
            _screenshotsMcs = new ScreenshotsMCS(director);
        }

        public string GenerateHeadImageString(int width, int height, HeadImageType imageType)
        {
            return Screenshots.GenerateImageString(GenerateHeadImage(width, height, imageType));
        }

        public Bitmap GenerateHeadImage(int width, int height, HeadImageType imageType)
        {
            _doc.Views.ActiveView = _doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            _visualizer.Reset();
            
            SetVisibilityForView(imageType);
            SetVisualizationForView(imageType);
            var cameraView = GetCameraView(imageType);
            _screenshotsMcs.SetCameraForView(cameraView);
            var bbox = GetBoundingBox(cameraView);

            var image = Screenshots.GenerateImage(_doc, width, height, bbox, true);
            image = _screenshotsMcs.CropBitmap(image, cameraView);

            _visualizer.Reset();
            _doc.Views.Redraw();
            return image;
        }

        private void SetVisibilityForView(HeadImageType imageType)
        {
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.ScapulaReamed].Layer
            };

            switch (imageType)
            {
                case HeadImageType.Superior:
                case HeadImageType.Anterior:
                case HeadImageType.Lateral:
                    showPaths.Add(BuildingBlocks.Blocks[IBB.Head].Layer);
                    break;
                case HeadImageType.LateralRBV:
                    showPaths.Add(BuildingBlocks.Blocks[IBB.RBVHead].Layer);
                    break;
            }

            Core.Visualization.Visibility.ResetTransparancies(_doc);
            Core.Visualization.Visibility.SetVisible(_doc, showPaths);
        }

        private void SetVisualizationForView(HeadImageType imageType)
        {
            switch (imageType)
            {
                case HeadImageType.Superior:
                    _visualizer.ShowHeadSuperiorView();
                    break;
                case HeadImageType.Anterior:
                    _visualizer.ShowHeadAnteriorView();
                    break;
                case HeadImageType.Lateral:
                    _visualizer.ShowHeadLateralView();
                    break;
            }
        }

        private CameraView GetCameraView(HeadImageType imageType)
        {
            var cameraView = CameraView.Lateral;
            switch (imageType)
            {
                case HeadImageType.Superior:
                    cameraView = CameraView.Superior;
                    break;
                case HeadImageType.Anterior:
                    cameraView = CameraView.Anterior;
                    break;
                case HeadImageType.Lateral:
                case HeadImageType.LateralReamed:
                case HeadImageType.LateralRBV:
                    cameraView = CameraView.Lateral;
                    break;
            }
            return cameraView;
        }

        private BoundingBox GetBoundingBox(CameraView cameraView)
        {
            var bbox = _doc.Objects.BoundingBoxVisible;
            switch (cameraView)
            {
                case CameraView.Anterior:
                case CameraView.Superior:
                    bbox.Transform(Transform.Translation(_anatomicalMeasurements.PlSagittal.Normal * 50));
                    break;
            }
            return bbox;
        }
    }
}