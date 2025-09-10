using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Display;
using System.Linq;

namespace IDS.Glenius.Visualization
{
    public static class View
    {
        public static bool SetIDSDefaults(RhinoDoc doc)
        {
            try
            {
                var perspectiveViewName = "Perspective";
                var frontViewName = "Front";
                // Set IDS settings
                SetViewToIDS(doc, perspectiveViewName);
                SetViewToIDS(doc, frontViewName);
                // Vertex shading
                SetViewVertexShading(doc, true, perspectiveViewName);
                SetViewVertexShading(doc, true, frontViewName);
                // Views
                var viewPerspective = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)[perspectiveViewName];
                var viewFront = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)[frontViewName];
                // Parallel perspective
                viewPerspective.ActiveViewport.ChangeToParallelProjection(true);
                // Turn off Cplane
                viewPerspective.ActiveViewport.ConstructionAxesVisible = false;
                viewPerspective.ActiveViewport.ConstructionGridVisible = false;
                viewFront.ActiveViewport.ConstructionAxesVisible = false;
                viewFront.ActiveViewport.ConstructionGridVisible = false;
                // Perspective view camera
                doc.Views.ActiveView = viewPerspective;
                MaximizePerspective(doc);
                //Unzoom all                
                doc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(doc.Objects.BoundingBox);
            }
            catch
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to set custom IDS visualisation. Please load custom Rhino Settings.");
                return false;
            }
            return true;
        }

        // Apply the rendered display mode
        public static bool SetViewToIDS(RhinoDoc doc, string viewName)
        {
            var view = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)[viewName];
            if (null == view)
            {
                return false;
            }

            var desc = DisplayModeDescription.FindByName("IDS");
            view.ActiveViewport.DisplayMode = desc;
            return true;
        }

        // Apply the rendered display mode
        public static bool SetViewVertexShading(RhinoDoc doc, bool enabled, string viewName)
        {
            var view = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)[viewName];
            if (null == view)
            {
                return false;
            }
            var desc = view.ActiveViewport.DisplayMode;
            desc.DisplayAttributes.ShadingEnabled = enabled;
            view.ActiveViewport.DisplayMode = desc;
            return true;
        }

        // Maximize the perspective view
        public static bool MaximizePerspective(RhinoDoc doc)
        {
            var perspectiveViewName = "Perspective";
            var viewPerspective = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)[perspectiveViewName];
            if (null == viewPerspective)
            {
                return false;
            }
            viewPerspective.Maximized = true;
            return true;
        }
    }
}