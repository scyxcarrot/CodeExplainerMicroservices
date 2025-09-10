using IDS.Core.PluginHelper;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Geometry;
using System.Drawing;
using System.Linq;

namespace IDS.Amace.Visualization
{
    internal class ScreenshotsSkirt
    {
        public static string GenerateSkirtImageString(RhinoDoc doc, int width, int height, CameraView view)
        {
            return Screenshots.GenerateImageString(GenerateSkirtImage(doc, width, height, view));
        }

        public static Bitmap GenerateSkirtImage(RhinoDoc doc, int width, int height, CameraView view)
        {
            // Check input data
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set view
            Visibility.SkirtQcDocumentImage(doc);
            View.SetView(doc, director.cup.centerOfRotation, view);
            return Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true);
        }
    }
}