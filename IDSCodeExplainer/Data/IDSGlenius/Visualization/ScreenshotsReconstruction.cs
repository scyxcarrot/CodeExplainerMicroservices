using IDS.Core.Drawing;
using IDS.Core.Visualization;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.Glenius.Visualization
{
    public class ScreenshotsReconstruction
    {
        private readonly RhinoDoc _doc;
        private readonly ScreenshotsMCS _screenshotsMcs;
        private readonly GleniusObjectManager _objectManager;
        private readonly QCDocReconstructionInformationVisualizer _visualizer;
        private readonly ColoredMeshConduit _meshConduit;

        public ScreenshotsReconstruction(GleniusImplantDirector director)
        {
            _doc = director.Document;
            _screenshotsMcs = new ScreenshotsMCS(director);
            _objectManager = new GleniusObjectManager(director);
            _visualizer = new QCDocReconstructionInformationVisualizer(director.AnatomyMeasurements);
            _meshConduit = new ColoredMeshConduit();
        }

        public void SetupVisualization()
        {
            var scapula = (Mesh)_objectManager.GetBuildingBlock(IBB.Scapula).Geometry.Duplicate();
            var nonDefectRegion = (Mesh)_objectManager.GetBuildingBlock(IBB.ScapulaDefectRegionRemoved).Geometry.Duplicate();
            var regionCurves = _objectManager.GetAllBuildingBlocks(IBB.DefectRegionCurves).Select(curve => (Curve)curve.Geometry);
            List<Mesh> scapulaParts;
            SplitWithCurve.OperatorSplitWithCurve(scapula, regionCurves.ToArray(), true, 100, 0.05, out scapulaParts);
            if (scapulaParts != null && scapulaParts.Count > 0)
            {
                var defectRegion = new Mesh();
                foreach (var part in scapulaParts)
                {
                    if (!nonDefectRegion.GetBoundingBox(false).Contains(part.GetBoundingBox(false)))
                    {
                        defectRegion.Append(part);
                    }
                }
                _meshConduit.AddMesh(defectRegion, Color.FromArgb(219, 66, 35));
            }
            _meshConduit.AddMesh(nonDefectRegion, Color.FromArgb(191, 255, 0));
        }

        public void ResetVisualization()
        {
            _meshConduit.Enabled = false;
            _visualizer.Reset();
        }

        public string GenerateReconstructionImageString(int width, int height, CameraView cameraView)
        {
            SetVisibilityForReconstruction(cameraView);
            return Screenshots.GenerateImageString(GenerateOverviewImage(width, height, cameraView));
        }

        public string GenerateDefectImageString(int width, int height, CameraView cameraView)
        {
            SetVisibilityForDefect();
            return Screenshots.GenerateImageString(GenerateOverviewImage(width, height, cameraView));
        }

        private Bitmap GenerateOverviewImage(int width, int height, CameraView cameraView)
        {
            _doc.Views.ActiveView = _doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];

            _screenshotsMcs.SetCameraForView(cameraView);

            var image = Screenshots.GenerateImage(_doc, width, height, _doc.Objects.BoundingBoxVisible, true);
            ResetVisualization();

            _doc.Views.Redraw();
            return image;
        }

        private void SetVisibilityForReconstruction(CameraView cameraView)
        {
            _visualizer.ShowComponents();
            switch (cameraView)
            {
                case CameraView.Anterior:
                case CameraView.Posterior:
                    _visualizer.ShowInclination();
                    break;
                case CameraView.Superior:
                    _visualizer.ShowVersion();
                    break;
            }

            var visuals = new Dictionary<IBB, double>
            {
                { IBB.ScapulaDefectRegionRemoved, 0.0 },
                { IBB.ReconstructedScapulaBone, 0.25 }
            };

            Visibility.SetTransparancies(_doc, visuals);
            Core.Visualization.Visibility.SetVisible(_doc, visuals.Select(visual => BuildingBlocks.Blocks[visual.Key].Layer).ToList());
        }

        private void SetVisibilityForDefect()
        {
            _meshConduit.Enabled = true;

            var visuals = new Dictionary<IBB, double>
            {
                { IBB.ReconstructedScapulaBone, 0.25 },
                { IBB.DefectRegionCurves, 0.0 }
            };

            Visibility.SetTransparancies(_doc, visuals);
            Core.Visualization.Visibility.SetVisible(_doc, visuals.Select(visual => BuildingBlocks.Blocks[visual.Key].Layer).ToList());
        }
    }
}