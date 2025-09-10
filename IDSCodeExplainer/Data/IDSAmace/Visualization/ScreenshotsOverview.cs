using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Geometry;
using System.Drawing;
using System.Linq;

namespace IDS.Amace.Visualization
{
    internal class ScreenshotsOverview
    {
        public static string GeneratePreOpImageString(RhinoDoc doc, int width, int height)
        {
            return Screenshots.GenerateImageString(GeneratePreOpImage(doc, width, height));
        }

        public static Bitmap GeneratePreOpImage(RhinoDoc doc, int width, int height)
        {
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set view
            Visibility.DefectOverview(doc);
            View.SetCupAcetabularView(doc);

            return Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true);
        }
        
        public static string GenerateOverviewImageString(RhinoDoc doc, int width, int height, CameraView view, DocumentType docType)
        {
            return Screenshots.GenerateImageString(GenerateOverviewImage(doc, width, height, view, docType));
        }

        public static Bitmap GenerateOverviewImage(RhinoDoc doc, int width, int height, CameraView view, DocumentType docType)
        {
            // Check input data
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            var PCS = director.Inspector.AxialPlane;
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set view
            if (docType == DocumentType.CupQC)
            {
                Visibility.CupQcImages(doc);
            }
            else
            {
                Visibility.ImplantQcDocumentOverview(doc);
            }
                
            View.SetView(doc, PCS.Origin, view);
            return Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true);
        }

        public static string GenerateBoneMeshImageString(RhinoDoc doc, int width, int height, CameraView view)
        {
            return Screenshots.GenerateImageString(GenerateBoneMeshImage(doc, width, height, view));
        }

        public static Bitmap GenerateBoneMeshImage(RhinoDoc doc, int width, int height, CameraView view)
        {
            // Check input data
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            var PCS = director.Inspector.AxialPlane;
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set view
            Visibility.BoneMeshComparison(doc);
            View.SetView(doc, PCS.Origin, view);

            return Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true);
        }

        public static string GenerateCollidablesImageString(RhinoDoc doc, int width, int height, CameraView view)
        {
            return Screenshots.GenerateImageString(GenerateCollidablesImage(doc, width, height, view));
        }

        public static Bitmap GenerateCollidablesImage(RhinoDoc doc, int width, int height, CameraView view)
        {
            // Check input data
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            var PCS = director.Inspector.AxialPlane;
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set view
            Visibility.CollidableEntities(doc);
            View.SetView(doc, PCS.Origin, view);

            return Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true);
        }
    }
}