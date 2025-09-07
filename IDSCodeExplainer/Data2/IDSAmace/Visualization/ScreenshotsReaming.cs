using IDS.Core.Visualization;
using Rhino;
using Rhino.Geometry;
using System.Drawing;
using System.Linq;

namespace IDS.Amace.Visualization
{
    internal class ScreenshotsReaming
    {
        public static string GenerateReamingImageString(RhinoDoc doc, int width, int height, bool showRBV, bool showTotal = false)
        {
            return Screenshots.GenerateImageString(GenerateReamingImage(doc, width, height, showRBV, showTotal));
        }

        public static Bitmap GenerateReamingImage(RhinoDoc doc, int width, int height, bool showRBV, bool showTotal = false)
        {
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set view
            if (showRBV && !showTotal)
                Visibility.ReamingPieces(doc);
            else if (showRBV)
                Visibility.ReamingTotal(doc);
            else
                Visibility.ReamedOriginalPelvis(doc);
            // Set camera for anterior view
            View.SetCupAcetabularView(doc);

            return Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true);
        }
    }
}