using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Proxies;
using IDS.Core.PluginHelper;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Geometry;
using System.Drawing;
using System.Linq;

namespace IDS.Amace.Visualization
{
    internal class ScreenshotsPlate
    {
        public static string GeneratePlateClearanceImageString(RhinoDoc doc, int width, int height, CameraView view, params Mesh[] visibleMeshes)
        {
            return Screenshots.GenerateImageString(GeneratePlateClearanceImage(doc, width, height, view, visibleMeshes));
        }

        public static Bitmap GeneratePlateClearanceImage(RhinoDoc doc, int width, int height, CameraView view, params Mesh[] visibleMeshes)
        {
            // Check input data
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            var PCS = director.Inspector.AxialPlane;
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set view
            Core.Visualization.Visibility.ResetTransparancies(doc);
            Core.Visualization.Visibility.HideAll(doc);
            var ids = visibleMeshes.Select(mesh => doc.Objects.AddMesh(mesh)).ToList();

            View.SetView(doc, PCS.Origin, view);
            var bitmap = Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true);

            foreach (var id in ids)
            {
                doc.Objects.Delete(id, true);
            }
            doc.Views.Redraw();

            return bitmap;
        }

        public static string GeneratePlateAngleImageString(RhinoDoc doc, int width, int height, CameraView view)
        {
            return Screenshots.GenerateImageString(GeneratePlateAngleImage(doc, width, height, view));
        }

        public static Bitmap GeneratePlateAngleImage(RhinoDoc doc, int width, int height, CameraView view)
        {
            // Check input data
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            var PCS = director.Inspector.AxialPlane;
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set view
            TogglePlateAnglesVisualisation.Enable(director);
            TogglePlateAnglesVisualisation.OnTop = true;
            Visibility.PlateDefault(doc);
            View.SetView(doc, PCS.Origin, view);

            // Create image
            var image = Screenshots.GenerateImage(doc, width, height, BoundingBox.Unset, true);
            // Disable
            TogglePlateAnglesVisualisation.Disable(director);
            // Refresh
            doc.Views.Redraw();

            return image;
        }

        public static string[][] GeneratePlateFeaImageStrings(RhinoDoc doc, int width, int height, Core.Fea.Fea fea, FeaConduit feaConduit, IBB referenceBuildingBlock, bool showBcAndLoad, double safetyFactorLow, double safetyFactorMiddle, double safetyFactorHigh, double materialFatigueLimit, double materialUTS)
        {
            // Select perspective viewport
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            // Set the view according to the reference object
            Core.Visualization.Visibility.ShowSingleBuildingBlock(doc, BuildingBlocks.Blocks[referenceBuildingBlock]);
            View.SetView(doc, fea.CameraTarget, fea.CameraTarget + fea.CameraDirection, fea.CameraUp);
            doc.Views.ActiveView.ActiveViewport.Magnify(0.9, false); // Add some extra margin
            doc.Views.Redraw();
            // Hide the reference object and turn the conduit on
            Core.Visualization.Visibility.HideAll(doc);
            feaConduit.SetVisualisationParameters(safetyFactorLow, safetyFactorMiddle, safetyFactorHigh, materialFatigueLimit, materialUTS);
            feaConduit.drawBoundaryConditions = showBcAndLoad;
            feaConduit.drawLoadMesh = showBcAndLoad;
            feaConduit.drawLegend = false;
            feaConduit.Enabled = true;
            doc.Views.Redraw();
            // Rotate the view and take a screenshot
            string[][] feaImageStrings = Screenshots.GenerateRotatingImageStrings(doc, width, height, 20, -120, 40, 40);
            // Turn the conduit off
            feaConduit.Enabled = false;
            doc.Views.Redraw();

            return feaImageStrings;
        }
    }
}