using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Geometry;
using System.Drawing;
using System.Linq;

namespace IDS.Amace.Visualization
{
    internal class ScreenshotsCup
    {
        public static string GenerateCupImageString(RhinoDoc doc, int width, int height, CupImageType imagetype, bool showOverlay = true)
        {
            return Screenshots.GenerateImageString(GenerateCupImage(doc, width, height, imagetype));
        }

        public static Bitmap GenerateCupImage(RhinoDoc doc, int width, int height, CupImageType imagetype, bool showOverlay = true, bool showImplant = false)
        {
            // Check input data
            ImplantDirector director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Create image
            Bitmap image = null;
            switch (imagetype)
            {
                case CupImageType.Anteversion:
                    image = GenerateCupAnteversionImage(doc, width, height, showOverlay, director);
                    break;
                case CupImageType.Inclination:
                    image = GenerateCupInclinationImage(doc, width, height, showOverlay, director);
                    break;
                case CupImageType.Position:
                    image = GenerateCupPositionImage(doc, width, height, showOverlay, showImplant, director);
                    break;
            }

            // Refresh
            doc.Views.Redraw();

            return image;
        }

        private static Bitmap GenerateCupPositionImage(RhinoDoc doc, int width, int height, bool showOverlay, bool showImplant, ImplantDirector director)
        {
            Bitmap image;
            // Camera
            doc.Views.ActiveView.ActiveViewport.ChangeToParallelProjection(true);
            View.SetPcsAnteriorView(doc);

            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // Lines conduit, has to be created to determine bounds
            Mesh def = objectManager.GetBuildingBlock(IBB.DefectPelvis).Geometry as Mesh;
            Mesh clat = new Mesh();
            if (objectManager.HasBuildingBlock(IBB.ContralateralPelvis))
                clat = objectManager.GetBuildingBlock(IBB.ContralateralPelvis).Geometry as Mesh;
            Mesh sacrum = new Mesh();
            if (objectManager.HasBuildingBlock(IBB.Sacrum))
                sacrum = objectManager.GetBuildingBlock(IBB.Sacrum).Geometry as Mesh;
            CupPositionConduit measLines = new CupPositionConduit(director.cup, director.CenterOfRotationContralateralFemur, director.CenterOfRotationDefectFemur, Color.Black, def, clat, sacrum, true, false, false);
            BoundingBox bnds = measLines.Bounds;
            bnds.Inflate(50);

            // Image, STLs Visualise necessary parts
            if (showImplant)
                Visibility.ImplantContralateralMeasurement(doc);
            else
                Visibility.CupContralateralMeasurement(doc);
            // Make image
            image = Screenshots.GenerateImage(doc, width, height, bnds, false);

            if (showOverlay)
            {
                // Lines
                measLines.LineThickness = 4;
                measLines.Enabled = true;
                // Hide all objects
                Core.Visualization.Visibility.HideAll(doc);
                // Make image (slightly smaller)
                Bitmap imageOverlayLines = Screenshots.GenerateImage(doc, width, height, bnds, false);
                // Disable conduit
                measLines.Enabled = false;
                // Merge
                Screenshots.AddOverlay(ref image, imageOverlayLines, Color.White);
                // Dispose
                imageOverlayLines.Dispose();

                // Text
                CupPositionConduit measText = new CupPositionConduit(director.cup, director.CenterOfRotationContralateralFemur, director.CenterOfRotationDefectFemur, Color.Black, def, clat, sacrum, false, false, true);
                measText.Enabled = true;
                // Hide all objects
                Core.Visualization.Visibility.HideAll(doc);
                // Make image (slightly smaller) to increase text size in result
                Bitmap imageOverlayText = Screenshots.GenerateImage(doc, 2 * width / 3, 2 * height / 3, bnds, false);
                // Disable conduit
                measText.Enabled = false;
                // Merge
                Screenshots.AddOverlay(ref image, imageOverlayText, Color.White);
                // Dispose
                imageOverlayText.Dispose();

                // Dots
                CupPositionConduit measDots = new CupPositionConduit(director.cup, director.CenterOfRotationContralateralFemur, director.CenterOfRotationDefectFemur, Color.Black, def, clat, sacrum, false, true, false);
                measDots.Enabled = true;
                // Hide all objects
                Core.Visualization.Visibility.HideAll(doc);
                // Make image (slightly smaller)
                Bitmap imageOverlayDots = Screenshots.GenerateImage(doc, width, height, bnds, false);
                // Disable conduit
                measDots.Enabled = false;
                // Merge
                Screenshots.AddOverlay(ref image, imageOverlayDots, Color.White);
                // Dispose
                imageOverlayDots.Dispose();
            }

            // Crop
            Bitmap trimmedImage = Screenshots.TrimBitmap(image);
            image = new Bitmap(trimmedImage);
            trimmedImage.Dispose();
            //image.MakeTransparent(Color.White);

            // Default cup visibility
            Visibility.CupDefault(doc);
            return image;
        }

        private static Bitmap GenerateCupInclinationImage(RhinoDoc doc, int width, int height, bool showOverlay, ImplantDirector director)
        {
            Bitmap image;
            // Conduit
            CupOrientationConduit orientationConduit = new CupOrientationConduit(director.cup);
            if (showOverlay)
            {
                orientationConduit.ShowInclination = true;
                orientationConduit.Enabled = true;
            }
            // Set visualisation
            View.SetCupAnteriorView(doc);
            Visibility.CupQcImages(doc);
            // Create image
            BoundingBox bbox = doc.Objects.BoundingBoxVisible;
            bbox.Inflate(50);
            image = Screenshots.GenerateImage(doc, width, height, bbox, true);
            // Disable
            if (showOverlay)
                orientationConduit.Enabled = false;
            return image;
        }

        private static Bitmap GenerateCupAnteversionImage(RhinoDoc doc, int width, int height, bool showOverlay, ImplantDirector director)
        {
            Bitmap image;
            // Conduit
            CupOrientationConduit orientationConduit = new CupOrientationConduit(director.cup);
            if (showOverlay)
            {
                orientationConduit.ShowAnteversion = true;
                orientationConduit.Enabled = true;
            }
            // Set visualisation
            View.SetCupInferiorView(doc);
            Visibility.CupQcImages(doc);
            // Create image
            BoundingBox bbox = doc.Objects.BoundingBoxVisible;
            bbox.Inflate(50);
            image = Screenshots.GenerateImage(doc, width, height, bbox, true);
            // Disable
            if (showOverlay)
                orientationConduit.Enabled = false;
            return image;
        }
    }
}