using IDS.Core.Operations;
using IDS.Core.Visualization;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.Linq;

namespace IDS.Glenius.Visualization
{
    public class ScreenshotsDesignScapula
    {
        private readonly RhinoDoc _doc;
        private readonly GleniusObjectManager _objectManager;
        private readonly ScreenshotsMCS _screenshotsMcs;
        private Mesh _meshDifference;
        private Guid _meshDifferenceId;

        public ScreenshotsDesignScapula(GleniusImplantDirector director)
        {
            _doc = director.Document;
            _objectManager = new GleniusObjectManager(director);
            _screenshotsMcs = new ScreenshotsMCS(director);
        }

        public void GenerateMeshDifference()
        {
            var scapulaDesign = (Mesh)_objectManager.GetBuildingBlock(IBB.ScapulaDesign).Geometry;
            var scapulaOriginal = (Mesh)_objectManager.GetBuildingBlock(IBB.Scapula).Geometry;
            _meshDifference = AnalysisMeshMaker.CreateDesignMeshDifference(scapulaDesign, scapulaOriginal);
        }

        public string GenerateDesignScapulaImageString(int width, int height, CameraView cameraView)
        {
            return Screenshots.GenerateImageString(GenerateDesignScapulaImage(width, height, cameraView));
        }

        public Bitmap GenerateDesignScapulaImage(int width, int height, CameraView cameraView)
        {
            _doc.Views.ActiveView = _doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];

            SetVisibilityForView();
            _screenshotsMcs.SetCameraForView(cameraView);

            var image = Screenshots.GenerateImage(_doc, width, height, _doc.Objects.BoundingBoxVisible, true);

            RemoveMeshDifference();
            _doc.Views.Redraw();
            return image;
        }

        private void SetVisibilityForView()
        {
            if (_meshDifference == null)
            {
                GenerateMeshDifference();
            }

            Core.Visualization.Visibility.ResetTransparancies(_doc);
            Core.Visualization.Visibility.HideAll(_doc);

            if (_meshDifferenceId != Guid.Empty)
            {
                RemoveMeshDifference();
            }

            _meshDifferenceId = _doc.Objects.AddMesh(_meshDifference);
        }

        private void RemoveMeshDifference()
        {
            if (_meshDifferenceId != Guid.Empty && _doc.Objects.Find(_meshDifferenceId) != null)
            {
                _doc.Objects.Show(_meshDifferenceId, true);
                _doc.Objects.Delete(_meshDifferenceId, true);
                _meshDifferenceId = Guid.Empty;
            }
        }
    }
}