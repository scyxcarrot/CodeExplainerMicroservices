using IDS.CMF.DataModel;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.Core.V2.Utilities;
using IDS.Core.V2.Visualization;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Visualization
{
    internal static class ScreenshotsUtilities
    {
        public static List<string> GetBonesPaths(string parentLayer)
        {
            return QCDocumentBonesQuery.GetBonesPaths(parentLayer);
        }

        public static List<string> GetTeethPaths(string parentLayer)
        {
            var subLayerNames = ProPlanImportUtilities.GetComponentSubLayerNames(ProPlanImportPartType.Teeth);
            return subLayerNames.Select(subLayer => $"{parentLayer}::{subLayer}").ToList();
        }

        private static List<string> GetImplantCaseComponentPaths(CMFImplantDirector director, IBB component)
        {
            var showPaths = new List<string>();
            var implantComponent = new ImplantCaseComponent();

            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                var extendedBuildingBlock = implantComponent.GetImplantBuildingBlock(component, casePreferenceData);
                showPaths.Add(extendedBuildingBlock.Block.Layer);
            }

            return showPaths;
        }

        private static List<string> GetGuideCaseComponentPaths(CMFImplantDirector director, IBB component)
        {
            var showPaths = new List<string>();
            var guideComponent = new GuideCaseComponent();

            foreach (var guidePreference in director.CasePrefManager.GuidePreferences)
            {
                var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(component, guidePreference);
                showPaths.Add(extendedBuildingBlock.Block.Layer);
            }

            return showPaths;
        }

        public static List<string> GetImplantPreviewPaths(CMFImplantDirector director)
        {
            return GetImplantCaseComponentPaths(director, IBB.ImplantPreview);
        }

        public static List<string> GetActualImplantPaths(CMFImplantDirector director)
        {
            return GetImplantCaseComponentPaths(director, IBB.ActualImplant);
        }

        public static List<string> GetGuidePreviewSmoothenPaths(CMFImplantDirector director)
        {
            return GetGuideCaseComponentPaths(director, IBB.GuidePreviewSmoothen);
        }

        public static List<string> GetActualGuidePaths(CMFImplantDirector director)
        {
            return GetGuideCaseComponentPaths(director, IBB.ActualGuide);
        }

        public static string GenerateImplantGuideImage(CMFImplantDirector director, BoundingBox bBox, Vector3d viewDirection, bool resize = true)
        {
            return GenerateImplantGuideImage(director, bBox, viewDirection, Color.White, resize);
        }

        public static string GenerateImplantGuideImage(CMFImplantDirector director, BoundingBox bBox, CameraView view, bool resize = true)
        {
            View.GetCameraPosition(director.Document, bBox.Center, 
                view, 1, out var cameraUp, out var cameraPosition);
            var viewDirection = cameraPosition - bBox.Center;
            return GenerateImplantGuideImage(director, bBox, viewDirection, cameraUp, Color.White, resize);
        }

        public static string GenerateImplantGuideImage(CMFImplantDirector director, BoundingBox bBox, Vector3d viewDirection, Color trimColor, bool resize = true)
        {
            var doc = director.Document;
            return GenerateImplantGuideImage(director, bBox, viewDirection, doc.Views.ActiveView.ActiveViewport.CameraUp, trimColor, resize);
        }

        public static string GenerateImplantGuideImage(CMFImplantDirector director, BoundingBox bBox, 
            Vector3d viewDirection, Vector3d cameraUp, Color trimColor, bool resize = true)
        {
            var doc = director.Document;
            ActivatePerspectiveViewForScreenshot(doc);

            double nearDistance, farDistance;
            doc.Views.ActiveView.ActiveViewport.GetDepth(doc.Views.ActiveView.ActiveViewport.GetFrustumBoundingBox(), out nearDistance, out farDistance);
            Point3d targetPos = bBox.Center + (viewDirection * farDistance);

            View.SetView(doc, bBox.Center, targetPos, cameraUp);
            return GenerateImplantGuideImage(director, bBox, trimColor, resize);
        }

        public static string GenerateImplantGuideImage(CMFImplantDirector director, BoundingBox bBox, Color trimColor, bool resize = true)
        {
            var doc = director.Document;
            doc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(bBox);

            int width;
            int height;
            GetWidthAndHeight(doc, resize, out width, out height);

            var image = Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true, "IDSCMF");
            if (trimColor != Color.White)
            {
                var imgTrim = Screenshots.TrimBitmap(image, trimColor.R, trimColor.G, trimColor.B);
                image.Dispose();
                image = new Bitmap(imgTrim);
                imgTrim.Dispose();
            }
            return Screenshots.GenerateImageString(image);
        }

        public static List<string> GenerateImplantGuideImages(RhinoDoc doc, BoundingBox bBox, List<CameraView> views, bool resize = true)
        {
            var imagesString = new List<string>();

            ActivatePerspectiveViewForScreenshot(doc);

            var viewportSize = doc.Views.ActiveView.ActiveViewport.Size;
            doc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(bBox);

            foreach (var view in views)
            {
                View.SetView(doc, bBox.Center, view);

                int width;
                int height;
                GetWidthAndHeight(doc, resize, out width, out height);

                var screenshotSize = new Size(width, height);
                doc.Views.ActiveView.ActiveViewport.Size = screenshotSize;

                var img = Screenshots.CaptureToBitmap(doc, screenshotSize, true);
                imagesString.Add(Screenshots.GenerateImageString(img));
            }

            doc.Views.ActiveView.ActiveViewport.Size = viewportSize;

            return imagesString;
        }

        private static void GetWidthAndHeight(RhinoDoc doc, bool resize, out int width, out int height)
        {
            width = doc.Views.ActiveView.ActiveViewport.Size.Width;
            height = doc.Views.ActiveView.ActiveViewport.Size.Height;

            if ((width < 1000 || height < 1000) && resize)
            {
                height = (int)(height * 1.5);
                width = (int)(width * 1.5);
            }
        }

        public static List<string> GenerateEmptyStrings(List<CameraView> views)
        {
            var imagesString = new List<string>();

            foreach (var view in views)
            {
                imagesString.Add(string.Empty);
            }

            return imagesString;
        }

        public static Mesh GenerateClearanceMeshForScreenshot(Mesh mesh, List<double> distances)
        {
            var clearanceMesh = mesh.DuplicateMesh();

            var clearanceScale = new ColorScale(
                new[]
                {
                    1, 1, 0, 0, 0, 0.49, 0.98
                },
                new[]
                {
                    0, 1, 1, 0.5, 0, 0.49, 0.98
                },
                new[]
                {
                    0, 0, 0, 0.5, 1, 0.99, 0.98
                });

            var colors = new List<Color>();

            for (var i = 0; i < distances.Count; i++)
            {
                if (distances[i] < 0)
                {
                    distances[i] = 0;

#if (INTERNAL)
                    if (CMFImplantDirector.IsDebugMode)
                    {
                        InternalUtilities.AddPoint(clearanceMesh.Vertices[i], $"TestQCDoc", $"NegativeClearance", Color.Red);
                    }
#endif
                }
            }

            foreach (var distance in distances)
            {
                var rgbInterpolation = DrawUtilitiesV2.InterpolateColorScale(distance, 0.0, 0.3, clearanceScale);
                var redEightBit = (int)(rgbInterpolation[0] * 255.0);
                var greenEightBit = (int)(rgbInterpolation[1] * 255.0);
                var blueEightBit = (int)(rgbInterpolation[2] * 255.0);
                colors.Add(Color.FromArgb(redEightBit, greenEightBit, blueEightBit));
            }

            clearanceMesh.VertexColors.SetColors(colors.ToArray());
            return clearanceMesh;
        }

        public static void ActivatePerspectiveViewForScreenshot(RhinoDoc doc)
        {
            var reset = View.ResetLayouts(doc);
            var views = doc.Views.Select(x => x).ToList();

            foreach (var view in views)
            {
                
                if (view.ActiveViewport.Name != View.PerspectiveViewName || view.ActiveViewport.DisplayMode.EnglishName != View.DisplayModeName)
                {
                    continue;
                }

                if (view.ActiveViewport.IsParallelProjection)
                {
                    doc.Views.ActiveView = view;
                    break;
                }
            }

            if (reset)
            {
                View.SetViewToIdsCmf(doc, View.PerspectiveViewName);
                View.MaximizePerspective(doc);
            }
        }

        public static List<string> GenerateBuildingBlockWithScrewsImagesString(CMFImplantDirector director, ExtendedImplantBuildingBlock extBuildingBlock, ExtendedImplantBuildingBlock screwBuildingBlock, 
            double screwBubbleRadius, double screwDisplaySize, List<CameraView> views)
        {
            var showPaths = new List<string>
            {
                extBuildingBlock.Block.Layer,
                screwBuildingBlock.Block.Layer
            };

            var doc = director.Document;
            Core.Visualization.Visibility.SetVisible(doc, showPaths);

            var objectManager = new CMFObjectManager(director);
            var geometry = objectManager.GetBuildingBlock(extBuildingBlock).Geometry;
            if (geometry == null)
            {
                return GenerateEmptyStrings(views);
            }

            var bBox = geometry.GetBoundingBox(true);

            return GenerateWithScrewsImagesString(director, bBox, screwBuildingBlock, screwBubbleRadius, screwDisplaySize, views);
        }

        private static List<string> GenerateWithScrewsImagesString(CMFImplantDirector director, BoundingBox bBox, ExtendedImplantBuildingBlock screwBuildingBlock,
            double screwBubbleRadius, double screwDisplaySize, List<CameraView> views)
        {
            if (!bBox.IsValid)
            {
                return GenerateEmptyStrings(views);
            }

            var objectManager = new CMFObjectManager(director);
            var bubbleConduits = new List<NumberBubbleConduit>();
            var screws = objectManager.GetAllBuildingBlocks(screwBuildingBlock).Select(s => (Screw)s).ToList();
            screws.ForEach(x =>
            {
                var conduit = new NumberBubbleConduit(x.HeadPoint, x.Index, Color.AliceBlue, ScrewUtilities.GetScrewTypeColor(x));
                conduit.BubbleRadius = screwBubbleRadius;
                conduit.DisplaySize = screwDisplaySize;
                bubbleConduits.Add(conduit);
            });

            foreach (var conduit in bubbleConduits)
            {
                conduit.Enabled = true;
            }

            var imagesString = GenerateImplantGuideImages(director.Document, bBox, views, false);

            foreach (var conduit in bubbleConduits)
            {
                conduit.Enabled = false;
            }

            bubbleConduits.Clear();

            return imagesString;
        }
    }
}
