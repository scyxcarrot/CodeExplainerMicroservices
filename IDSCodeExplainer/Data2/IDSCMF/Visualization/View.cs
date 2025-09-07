using IDS.CMF.FileSystem;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public class View : Core.Visualization.View
    {
        public const string PerspectiveViewName = "Perspective";
        public const string DisplayModeName = "IDSCMF";
        public const string DisplayMode2Name = "IDSCMF2";

        public static bool SetIDSDefaults(RhinoDoc doc)
        {
            try
            {
                const string frontViewName = "Front";
                // Set IDS settings
                SetViewToIdsCmf(doc, PerspectiveViewName);
                SetViewToIdsCmf(doc, frontViewName);
                // Vertex shading
                SetViewVertexShading(doc, true, PerspectiveViewName);
                SetViewVertexShading(doc, true, frontViewName);
                // Views
                var viewPerspective = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)[PerspectiveViewName];
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
            catch (Exception e)
            {
                Msai.TrackException(e, "CMF");
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to set custom IDS visualisation. Please load custom Rhino Settings.");
                return false;
            }
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
            var viewPerspective = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)[PerspectiveViewName];
            if (null == viewPerspective)
            {
                return false;
            }

            viewPerspective.Maximized = true;
            return true;
        }

        public static bool ResetLayouts(RhinoDoc doc)
        {
            if (!doc.Views.Any(v => v.ActiveViewport.Name == PerspectiveViewName))
            {
                RhinoApp.RunScript($"-_4View", false);
                return true;
            }
            return false;
        }

        public static bool SetViewToIdsCmf(RhinoDoc doc, string viewName)
        {
            var view = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)[viewName];
            if (null == view)
            {
                return false;
            }

            SetDisplayModeToIdsCmf(view);
            return true;
        }

        public static bool GetCameraPosition(RhinoDoc doc, Point3d cameraTarget, CameraView view,
            double cameraDistance, out Vector3d cameraUp, out Point3d cameraPosition)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);
            var mcs = director.MedicalCoordinateSystem;

            cameraUp = Vector3d.ZAxis; // default
            cameraPosition = Point3d.Origin; // default

            switch (view)
            {
                case CameraView.Front:
                    cameraUp = mcs.AxialPlane.ZAxis;
                    cameraPosition = cameraTarget - mcs.CoronalPlane.ZAxis * cameraDistance;
                    break;

                case CameraView.FrontLeft:
                    cameraUp = mcs.AxialPlane.ZAxis;
                    cameraPosition = cameraTarget + (-1.5 * mcs.CoronalPlane.ZAxis - 0.5 * mcs.SagittalPlane.ZAxis) / 2 * cameraDistance;
                    break;

                case CameraView.Left:
                    cameraUp = mcs.AxialPlane.ZAxis;
                    cameraPosition = cameraTarget + (-mcs.CoronalPlane.ZAxis - mcs.SagittalPlane.ZAxis) / 2 * cameraDistance;
                    break;

                case CameraView.FrontRight:
                    cameraUp = mcs.AxialPlane.ZAxis;
                    cameraPosition = cameraTarget + (-1.5 * mcs.CoronalPlane.ZAxis + 0.5 * mcs.SagittalPlane.ZAxis) / 2 * cameraDistance;
                    break;

                case CameraView.Right:
                    cameraUp = mcs.AxialPlane.ZAxis;
                    cameraPosition = cameraTarget + (-mcs.CoronalPlane.ZAxis + mcs.SagittalPlane.ZAxis) / 2 * cameraDistance;
                    break;

                case CameraView.Back:
                    cameraUp = mcs.AxialPlane.ZAxis;
                    cameraPosition = cameraTarget + mcs.CoronalPlane.ZAxis * cameraDistance;
                    break;

                case CameraView.NegateLeft:
                    cameraUp = mcs.AxialPlane.ZAxis;
                    cameraPosition = cameraTarget - (-mcs.CoronalPlane.ZAxis - mcs.SagittalPlane.ZAxis) / 2 * cameraDistance;
                    break;

                case CameraView.NegateRight:
                    cameraUp = mcs.AxialPlane.ZAxis;
                    cameraPosition = cameraTarget - (-mcs.CoronalPlane.ZAxis + mcs.SagittalPlane.ZAxis) / 2 * cameraDistance;
                    break;

                case CameraView.Top:
                    cameraUp = mcs.CoronalPlane.ZAxis;
                    cameraPosition = cameraTarget + mcs.AxialPlane.ZAxis * cameraDistance;
                    break;

                case CameraView.Bottom:
                    cameraUp = mcs.CoronalPlane.ZAxis;
                    cameraPosition = cameraTarget - mcs.AxialPlane.ZAxis * cameraDistance;
                    break;

                default:
                    return false;
            }

            return true;
        }

        public static void SetView(RhinoDoc doc, Point3d cameraTarget, Point3d cameraPosition, Vector3d cameraUp)
        {
            var currentView = doc.Views.ActiveView;
            currentView.ActiveViewport.SetCameraLocations(Point3d.Origin, (Point3d)Vector3d.YAxis); // reset
            currentView.ActiveViewport.SetCameraLocations(cameraTarget, cameraPosition);
            currentView.ActiveViewport.CameraUp = cameraUp;
            currentView.ActiveViewport.ZoomExtents();
        }

        public static void SetView(RhinoDoc doc, Point3d cameraTarget, CameraView view)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);
            var mcs = director.MedicalCoordinateSystem;

            var cameraDistance = 580.0;

            GetCameraPosition(doc, cameraTarget, view, cameraDistance, out var cameraUp, out var cameraPosition);

            SetView(doc, cameraTarget, cameraPosition, cameraUp);
        }

        public static bool SetSagittalView(RhinoDoc doc)
        {
            var sagittalPlane = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId).MedicalCoordinateSystem.SagittalPlane;
            var axialPlane = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId).MedicalCoordinateSystem.AxialPlane;
            return SetPlaneView(doc, sagittalPlane, axialPlane.Normal);
        }

        public static bool SetAxialView(RhinoDoc doc)
        {
            var axialPlane = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId).MedicalCoordinateSystem.AxialPlane;
            var coronalPlane = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId).MedicalCoordinateSystem.CoronalPlane;
            return SetPlaneView(doc, axialPlane, coronalPlane.Normal);
        }

        public static bool SetCoronalView(RhinoDoc doc)
        {
            var coronalPlane = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId).MedicalCoordinateSystem.CoronalPlane;
            var axialPlane = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId).MedicalCoordinateSystem.AxialPlane;
            return SetPlaneView(doc, coronalPlane, axialPlane.Normal);
        }

        public static bool SetPlaneView(RhinoDoc doc, Plane plane, Vector3d cameraUp)
        {
            try
            {
                var cameraTarget = plane.Origin;
                var cameraDirection = -plane.Normal;

                const double cameraDistance = 580.0;
                var cameraPosition = cameraTarget - cameraDirection * cameraDistance;
                SetView(doc, cameraTarget, cameraPosition, cameraUp);
            }
            catch (Exception e)
            {
                Msai.TrackException(e, "CMF");
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not set the view.");
                return false;
            }
            return true;
        }

        public static void ImportIDSCMF2DisplayMode()
        {
            var resources = new CMFResources();
            DisplayModeDescription.ImportFromFile(resources.IdsCmf2SettingsFile);
        }

        public static void SetDisplayModeToIdsCmf(RhinoDoc doc)
        {
            SetDisplayModeToIdsCmf(doc.Views.ActiveView);
        }

        public static void SetDisplayModeToIdsCmf(RhinoView view)
        {
            var desc = DisplayModeDescription.FindByName(DisplayModeName);
            view.ActiveViewport.DisplayMode = desc;
            view.Redraw();
        }
    }
}