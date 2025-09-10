using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Geometry;
using System.Linq;


namespace IDS.Amace.Visualization
{
    public class View : Core.Visualization.View
    {
        // Generic set view function (to be used by convenience functions)
        // Note: cameraTarget is ignored if view is set to acetabular
        public static Plane SetView(RhinoDoc doc, Point3d cameraTarget, CameraView view, bool setCameraToView = true)
        {
            // Check if all needed data is available
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            var inspector = director.Inspector;
            var cup = director.cup;
            var pcs = inspector.AxialPlane;

            var cameraDistance = 580.0;
            var cameraUp = Vector3d.ZAxis; // default
            var cameraPosition = Point3d.Origin; // default
            var sideVector = director.defectIsLeft ? -pcs.YAxis : pcs.YAxis; // points to medial

            switch (view)
            {
                case CameraView.Anterior:
                    cameraUp = pcs.ZAxis;
                    cameraPosition = cameraTarget + pcs.XAxis * cameraDistance;
                    break;

                case CameraView.Lateral:
                    cameraUp = pcs.ZAxis;
                    cameraPosition = cameraTarget - sideVector * cameraDistance;
                    break;

                case CameraView.Medial:
                    cameraUp = pcs.ZAxis;
                    cameraPosition = cameraTarget + sideVector * cameraDistance;
                    break;

                case CameraView.Inferior:
                    cameraUp = pcs.XAxis;
                    cameraPosition = cameraTarget - pcs.ZAxis * cameraDistance;
                    break;

                case CameraView.Inferolateral:
                    cameraUp = 0.9320 * pcs.ZAxis + 0.3624 * sideVector;
                    cameraUp.Unitize();
                    cameraPosition = cameraTarget - pcs.ZAxis * cameraDistance * 0.3624 - (sideVector * cameraDistance * 0.9320);
                    break;

                case CameraView.Acetabular:
                    cameraUp = pcs.ZAxis;
                    cameraPosition = cup.centerOfRotation + cup.orientation * cameraDistance;
                    break;

                case CameraView.Acetabularinverse:
                    cameraUp = pcs.ZAxis;
                    cameraPosition = cup.centerOfRotation - cup.orientation * cameraDistance;
                    break;

                case CameraView.Insertion:
                    cameraUp = pcs.ZAxis;
                    cameraPosition = cup.centerOfRotation - director.InsertionDirection * cameraDistance;
                    break;

                case CameraView.Insertioninverse:
                    cameraUp = pcs.ZAxis;
                    cameraPosition = cup.centerOfRotation + director.InsertionDirection * cameraDistance;
                    break;

                case CameraView.Illium:
                    // Based on report Lise D'Hoop
                    cameraUp = pcs.ZAxis;
                    cameraPosition = cup.centerOfRotation - (-MathUtilities.AnteversionInclinationToVector(-28.43, 79.28, pcs, director.defectIsLeft) * cameraDistance);
                    break;

                case CameraView.ContourPlane:
                    cameraUp = director.ContourPlane.YAxis;
                    cameraPosition = cup.centerOfRotation + director.ContourPlane.Normal * cameraDistance;
                    break;
            }

            if (view != CameraView.Inferolateral)
            {
                cameraUp = CalculateRealUp(cameraUp, cameraPosition, cameraTarget);
            }

            // Set the view
            if (setCameraToView)
            {
                SetView(doc, cameraTarget, cameraPosition, cameraUp);
            }

            //View Plane to subject, Y is the up vector, Normal is the look vector
            //IMPORTANT: IDS will save the Originm XAxis and YAxis in the document and is loaded using this!
            //If the XAxis and YAxis is changed, the normal will change!
            var viewDirection = cameraTarget - cameraPosition;
            viewDirection.Unitize();
            var viewPlane = new Plane(cameraTarget, viewDirection)
            {
                YAxis = cameraUp,
                XAxis = Vector3d.CrossProduct(cameraUp, viewDirection)
            };

            return viewPlane;
        }

        public static void SetView(RhinoDoc doc, Point3d cameraTarget, Vector3d cameraDirection, Vector3d cameraUp)
        {
            const double cameraDistance = 580.0;
            var cameraPosition = cameraTarget - cameraDirection * cameraDistance;
            SetView(doc, cameraTarget, cameraPosition, cameraUp);
        }

        public static void SetView(RhinoDoc doc, Point3d cameraTarget, Point3d cameraPosition, Vector3d cameraUp)
        {
            var currentView = doc.Views.ActiveView;
            currentView.ActiveViewport.SetCameraLocations(Point3d.Origin, (Point3d)Vector3d.YAxis); // reset
            currentView.ActiveViewport.SetCameraLocations(cameraTarget, cameraPosition);
            currentView.ActiveViewport.CameraUp = cameraUp;
            currentView.ActiveViewport.ZoomExtents();
        }

        private static Vector3d CalculateRealUp(Vector3d up, Point3d position, Point3d target)
        {
            up.Unitize();
            var vPt = target - position;
            vPt.Unitize();
            var vC = Vector3d.CrossProduct(up, vPt);
            vC.Unitize();
            var vRealUp = Vector3d.CrossProduct(vC, vPt);
            vRealUp.Unitize();

            if (Vector3d.VectorAngle(up, vRealUp) > System.Math.PI / 2)
            {
                vRealUp = -vRealUp;
            }

            return vRealUp;
        }

        public static bool SetIDSDefaults(RhinoDoc doc)
        {
            try
            {
                const string perspectiveViewName = "Perspective";
                const string frontViewName = "Front";
                const string displayModeName = "IDS";

                // Set IDS settings
                SetViewAndDisplayMode(doc, perspectiveViewName, displayModeName);
                SetViewAndDisplayMode(doc, frontViewName, displayModeName);
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
                SetPcsAnteriorView(doc);
            }
            catch
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to set custom IDS visualisation. Please load custom Rhino Settings.");
                return false;
            }
            return true;
        }

        // Overview from anterior
        public static bool SetPcsAnteriorView(RhinoDoc doc)
        {
            try
            {
                var pcs = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId).Inspector.AxialPlane;
                SetView(doc, pcs.Origin, CameraView.Anterior);
            }
            catch
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not set the view.");
                return false;
            }
            return true;
        }

        // Cup detail from inferior
        public static bool SetCupInferiorView(RhinoDoc doc)
        {
            try
            {
                var cup = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId).cup;
                SetView(doc, cup.centerOfRotation, CameraView.Inferior);
            }
            catch
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not set the view.");
                return false;
            }
            return true;
        }

        // Cup detail from anterior
        public static bool SetCupAnteriorView(RhinoDoc doc)
        {
            try
            {
                var cup = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId).cup;
                SetView(doc, cup.centerOfRotation, CameraView.Anterior);
            }
            catch
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not set the view.");
                return false;
            }
            return true;
        }

        // Cup detail, perpendicular to cup rim plane
        public static bool SetCupAcetabularView(RhinoDoc doc)
        {
            try
            {
                var cup = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId).cup;
                SetView(doc, cup.centerOfRotation, CameraView.Acetabular);
            }
            catch
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not set the view.");
                return false;
            }
            return true;
        }

        // Cup detail, along insertion direction
        public static bool SetCupInsertionView(RhinoDoc doc)
        {
            try
            {
                var cup = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId).cup;
                SetView(doc, cup.centerOfRotation, CameraView.Insertion);
            }
            catch
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not set the view.");
                return false;
            }
            return true;
        }

        public static bool SetContourPlaneView(RhinoDoc doc)
        {
            try
            {
                var cup = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId).cup;
                SetView(doc, cup.centerOfRotation, CameraView.ContourPlane);
            }
            catch
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not set the view.");
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
            const string perspectiveViewName = "Perspective";
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