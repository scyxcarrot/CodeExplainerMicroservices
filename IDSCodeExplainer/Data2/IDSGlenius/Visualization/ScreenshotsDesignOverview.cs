using IDS.Core.Visualization;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.Glenius.Visualization
{
    public class ScreenshotsDesignOverview
    {
        private readonly RhinoDoc _doc;
        private readonly ScreenshotsMCS _screenshotsMcs;

        public ScreenshotsDesignOverview(GleniusImplantDirector director)
        {
            _doc = director.Document;
            _screenshotsMcs = new ScreenshotsMCS(director);
        }

        public string GeneratePreOpOverviewImageString(int width, int height, CameraView cameraView)
        {
            SetVisibilityForPreOpOverview();
            return Screenshots.GenerateImageString(GenerateOverviewImage(width, height, cameraView));
        }

        public string GenerateDesignOverviewImageString(int width, int height, CameraView cameraView)
        {
            SetVisibilityForDesignOverview();
            return Screenshots.GenerateImageString(GenerateOverviewImage(width, height, cameraView));
        }

        private Bitmap GenerateOverviewImage(int width, int height, CameraView cameraView)
        {
            _doc.Views.ActiveView = _doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];

            _screenshotsMcs.SetCameraForView(cameraView);

            var image = Screenshots.GenerateImage(_doc, width, height, _doc.Objects.BoundingBoxVisible, true);

            _doc.Views.Redraw();
            return image;
        }

        private void SetVisibilityForPreOpOverview()
        {
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.HumerusBoneFragments].Layer,
                BuildingBlocks.Blocks[IBB.HumeralHead].Layer,
                BuildingBlocks.Blocks[IBB.Humerus].Layer,
                BuildingBlocks.Blocks[IBB.ScapulaBoneFragments].Layer,
                BuildingBlocks.Blocks[IBB.Scapula].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(_doc);
            Core.Visualization.Visibility.SetVisible(_doc, showPaths);
        }

        private void SetVisibilityForDesignOverview()
        {
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.Head].Layer,
                BuildingBlocks.Blocks[IBB.ScapulaReamed].Layer,
                BuildingBlocks.Blocks[IBB.CylinderHat].Layer,
                BuildingBlocks.Blocks[IBB.ProductionRod].Layer,
                BuildingBlocks.Blocks[IBB.M4ConnectionScrew].Layer,
                BuildingBlocks.Blocks[IBB.Screw].Layer,
                BuildingBlocks.Blocks[IBB.PlateBasePlate].Layer,
                BuildingBlocks.Blocks[IBB.ScaffoldTop].Layer,
                BuildingBlocks.Blocks[IBB.ScaffoldSide].Layer,
                BuildingBlocks.Blocks[IBB.ScaffoldBottom].Layer
            };

            Core.Visualization.Visibility.ResetTransparancies(_doc);
            Core.Visualization.Visibility.SetVisible(_doc, showPaths);
        }
    }
}