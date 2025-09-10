using IDS.CMF.Constants;
using IDS.Core.PluginHelper;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.Visualization
{
    internal static class ScreenshotsOverview
    {
        public static List<string> GenerateImplantPreviewOverviewImagesString(RhinoDoc doc, int width, int height, List<CameraView> views)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);

            var implantPaths = ScreenshotsUtilities.GetImplantPreviewPaths(director);
            var showPaths = GetBonesAndTeethPaths(ProPlanImport.PlannedLayer);
            showPaths.AddRange(implantPaths);
           
            return GenerateOverviewImagesString(doc, width, height, showPaths, views);
        }

        public static List<string> GenerateGuidePreviewOverviewImagesString(RhinoDoc doc, int width, int height, List<CameraView> views)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);

            var guidePaths = ScreenshotsUtilities.GetGuidePreviewSmoothenPaths(director);
            var showPaths = GetBonesAndTeethPaths(ProPlanImport.OriginalLayer);
            showPaths.AddRange(guidePaths);

            return GenerateOverviewImagesString(doc, width, height, showPaths, views);
        }

        public static List<string> GenerateActualImplantOverviewImagesString(RhinoDoc doc, int width, int height, List<CameraView> views)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);

            var implantPaths = ScreenshotsUtilities.GetActualImplantPaths(director);
            var showPaths = GetBonesAndTeethPaths(ProPlanImport.PlannedLayer);
            showPaths.AddRange(implantPaths);

            return GenerateOverviewImagesString(doc, width, height, showPaths, views);
        }

        public static List<string> GenerateActualGuideOverviewImagesString(RhinoDoc doc, int width, int height, List<CameraView> views)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);

            var guidePaths = ScreenshotsUtilities.GetActualGuidePaths(director);
            var showPaths = GetBonesAndTeethPaths(ProPlanImport.OriginalLayer);
            showPaths.AddRange(guidePaths);

            return GenerateOverviewImagesString(doc, width, height, showPaths, views);
        }

        private static int GetUpdatedWidth(BoundingBox bBox, RhinoDoc doc, int initialWidth)
        {
            var width = initialWidth;

            //adjust the width so that bigger implant/guide will not get cropped out
            var corners = bBox.GetCorners();
            var points = new List<System.Drawing.Point>();
            foreach (var corner in corners)
            {
                var clientPoint = doc.Views.ActiveView.ActiveViewport.WorldToClient(corner);
                points.Add(doc.Views.ActiveView.ActiveViewport.ClientToScreen(clientPoint));
            }

            var screenWidth = points.Max(p => p.X) - points.Min(p => p.X);
            var screenHeight = points.Max(p => p.Y) - points.Min(p => p.Y);
            var ratio = (double)screenWidth / (double)screenHeight;
            if (!double.IsInfinity(ratio) && ratio > 1.5)
            {
                width = (int)(width * ratio);
            }

            return width;
        }

        private static List<string> GenerateOverviewImagesString(RhinoDoc doc, int width, int height, List<string> showPaths, List<CameraView> views)
        {
            var imagesString = new List<string>();

            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);

            Core.Visualization.Visibility.SetVisible(doc, showPaths);

            var bBox = GetBoundingBox(doc, showPaths);

            ScreenshotsUtilities.ActivatePerspectiveViewForScreenshot(doc);

            var viewportSize = doc.Views.ActiveView.ActiveViewport.Size;

            foreach (var view in views)
            {
                View.SetView(doc, director.MedicalCoordinateSystem.AxialPlane.Origin, view);

                width = GetUpdatedWidth(bBox, doc, width);

                var screenshotSize = new Size(width, height);
                doc.Views.ActiveView.ActiveViewport.Size = screenshotSize;

                var img = Screenshots.CaptureToBitmap(doc, screenshotSize, true);
                imagesString.Add(Screenshots.GenerateImageString(img));
            }

            doc.Views.ActiveView.ActiveViewport.Size = viewportSize;

            return imagesString;
        }

        private static List<string> GetBonesAndTeethPaths(string parentLayer)
        {
            var showPaths = ScreenshotsUtilities.GetBonesPaths(parentLayer);
            showPaths.AddRange(ScreenshotsUtilities.GetTeethPaths(parentLayer));
            return showPaths;
        }

        private static BoundingBox GetBoundingBox(RhinoDoc doc, List<string> layerPaths)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);

            var bBox = BoundingBox.Empty;
            var objectManager = new CMFObjectManager(director);
            foreach (var path in layerPaths)
            {
                var objects = objectManager.GetAllObjectsByLayerPath(path);
                foreach (var o in objects)
                {
                    bBox.Union(o.Geometry.GetBoundingBox(true));
                }
            }

            return bBox;
        }
    }
}