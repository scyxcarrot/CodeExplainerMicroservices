using IDS.Amace.Enumerators;
using IDS.Core.DataTypes;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Visualization;
using IDS.Visualization;
using Rhino;
using Rhino.Geometry;
using System.Drawing;
using System.Linq;

namespace IDS.Amace.Visualization
{
    internal class ScreenshotsScrews
    {
        public static string GenerateScrewNumberImageString(RhinoDoc doc, int width, int height, CameraView view, ScrewConduitMode screwConduitMode, bool includePelvis, DocumentType docType)
        {
            return Screenshots.GenerateImageString(GenerateScrewNumberImage(doc, width, height, view, screwConduitMode, includePelvis, docType));
        }

        public static Bitmap GenerateScrewNumberImage(RhinoDoc doc, int width, int height, CameraView view, ScrewConduitMode screwConduitMode, bool includePelvis, DocumentType docType)
        {
            // Check input data
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set view
            var designPhase = docType == DocumentType.ImplantQC
                ? DesignPhase.ImplantQC
                : docType == DocumentType.CupQC
                    ? DesignPhase.CupQC
                    : DesignPhase.Export;
            var numbers = new ScrewConduit(director, screwConduitMode, designPhase) {Enabled = true};

            if (includePelvis)
            {
                Visibility.ScrewNumbers(doc);
            }
            else
            {
                Visibility.ScrewsAndPlateHoles(doc);
            }
            
            View.SetView(doc, director.cup.centerOfRotation, view);

            // Create image
            var image = Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true);
            // Disable conduit
            numbers.Enabled = false;
            // Refresh
            doc.Views.Redraw();

            return image;
        }

        public static string GenerateScrewBumpImageString(RhinoDoc doc, int width, int height)
        {
            return Screenshots.GenerateImageString(GenerateScrewBumpImage(doc, width, height));
        }

        public static Bitmap GenerateScrewBumpImage(RhinoDoc doc, int width, int height)
        {
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set view
            Visibility.ScrewBumps(doc);
            View.SetCupAcetabularView(doc);

            // Create image
            var image = Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true);
            // Refresh
            doc.Views.Redraw();

            return image;
        }
    }
}