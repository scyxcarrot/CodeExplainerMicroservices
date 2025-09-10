using IDS.Core.PluginHelper;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Geometry;
using System.Drawing;
using System.Linq;

namespace IDS.Amace.Visualization
{
    class ScreenshotsBoneGraft
    {
        public static string GenerateBoneGraftImageString(RhinoDoc doc, int width, int height, CameraView view)
        {
            return Screenshots.GenerateImageString(GenerateBoneGraftImage(doc, width, height, view));
        }

        public static Bitmap GenerateBoneGraftImage(RhinoDoc doc, int width, int height, CameraView view)
        {
            // Check input data
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            var pcs = director.Inspector.AxialPlane;
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set view
            Visibility.BoneGraftsOnPreopPelvis(doc);
            View.SetView(doc, pcs.Origin, view);

            return Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true);
        }

        public static string GenerateOriginalPelvisImageString(RhinoDoc doc, int width, int height, CameraView view)
        {
            return Screenshots.GenerateImageString(GenerateOriginalPelvisImage(doc, width, height, view));
        }

        public static Bitmap GenerateOriginalPelvisImage(RhinoDoc doc, int width, int height, CameraView view)
        {
            // Check input data
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            var pcs = director.Inspector.AxialPlane;
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set view
            Visibility.OriginalPelvis(doc);
            View.SetView(doc, pcs.Origin, view);

            return Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true);
        }

        public static string GenerateBoneGraftDifferenceMapImageString(RhinoDoc doc, Mesh differenceMesh, int width, int height, CameraView view)
        {
            return Screenshots.GenerateImageString(Screenshots.GenerateMeshImage(doc, differenceMesh, width, height, view));
        }
    }
}
