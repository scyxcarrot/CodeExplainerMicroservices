using IDS.Core.Utilities;
using IDS.Glenius;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Linq;

namespace IDS.Common.Operations
{
    /*
     * GetScrew provides functionality screw placement and screw repositioning
     */

    public class ScrewCreator : IDisposable
    {
        // Member variables
        private Plane headPlacementPlane;

        private Mesh targetMeshTip;
        private Point3d headPoint = Point3d.Unset;
        private Point3d tipPoint = Point3d.Unset;
        private GleniusImplantDirector director;
        private ScrewType screwType;
        private double fixedLength = 0.0;
        private readonly ScrewManager screwManager;

        public ScrewPlacementMethodType PlacementMethod { get; set; }

        private readonly Rhino.Display.DisplayMaterial drawScrewMaterial = new Rhino.Display.DisplayMaterial();

        private void CopyProperties(GleniusImplantDirector director, ScrewPlacementMethodType placementMethod, Plane headPlacementPlane, Mesh targetMeshTip, Screw other)
        {
            // director
            this.director = director;

            this.PlacementMethod = placementMethod;

            // target meshes
            this.headPlacementPlane = headPlacementPlane;
            this.targetMeshTip = targetMeshTip;
            this.targetMeshTip.FaceNormals.ComputeFaceNormals();

            // Get properties from the oldScrew
            headPoint = other.HeadPoint;
            tipPoint = other.TipPoint;
            screwType = other.ScrewType;
            fixedLength = other.FixedLength;

            // Screw material settings
            drawScrewMaterial.Ambient = Colors.MetalScrew;
            drawScrewMaterial.Specular = Colors.MetalScrew;
            drawScrewMaterial.Diffuse = Colors.MetalScrew;
            drawScrewMaterial.Transparency = 0.5;
            drawScrewMaterial.Shine = 0.5;
        }

        /**
         * Make a screw in any location.
         */

        /// <summary>
        /// Initializes a new instance of the <see cref="GetScrew"/> class.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="placementMethod">The placement method.</param>
        /// <param name="targetMeshHead">The target mesh head.</param>
        /// <param name="targetMeshTip">The target mesh tip.</param>
        /// <param name="type">The type of screw.</param>
        public ScrewCreator(GleniusImplantDirector director, ScrewPlacementMethodType placementMethod, Plane headPlacementPlane, Mesh targetMeshTip, ScrewType type)
        {
            screwManager = director.ScrewObjectManager;
            var screwTmp = new Screw(director, type, 0.0, -1); //to get pre calculated stuffs.. not a good way
            CopyProperties(director, placementMethod, headPlacementPlane, targetMeshTip, screwTmp);
        }
        /**
         * Empty constructor
         **/

        public ScrewCreator()
        {
            // empty
        }

        /**
         * Position screw
         **/

        public Screw Get()
        {
            // Init the return screw
            Screw theScrew;

            //Preload first as it requires to load only once per screw create call, not everytime it is adjusted
            ScrewBrepComponentDatabase.PreLoadScrewHead(screwType);

            // Check available data and start required screw get function
            switch (PlacementMethod)
            {
                // Place a screw using the view direction as screw axis
                case ScrewPlacementMethodType.CAMERA:
                    headPoint = Point3d.Unset;
                    tipPoint = Point3d.Unset;
                    theScrew = GetCamera();

                    break;

                // Place a screw by defining a head and a tip point
                case ScrewPlacementMethodType.TWO_POINTS:
                    headPoint = Point3d.Unset;
                    tipPoint = Point3d.Unset;
                    theScrew = GetInOut();
                    break;

                default:
                    return null;
            }

            // Set the return screw index to the old one
            if (null != theScrew)
            {
                screwManager.HandleIndexAssignment(ref theScrew);
            }

            // Return the screw
            return theScrew;
        }

        /**
         * Create a new screw by clicking the head and positioning it along the camera axis
         */

        public Screw GetCamera()
        {
            GetPoint gpts = new GetPoint();
            gpts.SetCommandPrompt("Select head point to position the screw along the view direction.");
            gpts.Constrain(headPlacementPlane, false);
            gpts.PermitObjectSnap(false);
            gpts.DynamicDraw += DrawScrew;
            gpts.AcceptNothing(true); // accept ENTER to confirm
            gpts.EnableTransparentCommands(false);
            while (true)
            {
                GetResult get_res = gpts.Get(); // function only returns after clicking
                if (get_res == GetResult.Cancel)
                {
                    return null;
                }

                if (get_res == GetResult.Point)
                {
                    // Set head point
                    headPoint = gpts.Point();

                    // Shoot a ray and find the first intersection, fixed minimum length if none is found
                    Vector3d rayDirection = director.Document.Views.ActiveView.ActiveViewport.CameraDirection;
                    rayDirection.Unitize();

                    Line line = new Line(headPoint, headPoint + (rayDirection * 500));

                    int[] faceIds;
                    var intersectionPoints = Intersection.MeshLine(targetMeshTip, line, out faceIds);

                    var point = PointUtilities.FindFurthermostPointAlongVector(intersectionPoints, rayDirection);

                    if (point != Point3d.Unset)
                    {
                        var screw = new Screw(director, headPoint, point, screwType, -1);

                        var calib = new ScrewCalibrator(screw, targetMeshTip, director);
                        calib.DoCalibration();

                        return calib.CalibratedScrew;
                    }
                    else
                    {
                        double maxScrewLength = ScrewBrepFactory.GetAvailableScrewLengths(screwType).Max();
                        tipPoint = headPoint + rayDirection * maxScrewLength;
                        var screw = new Screw(director, headPoint, tipPoint, screwType, -1);

                        var calib = new ScrewCalibrator(screw, targetMeshTip, director);
                        calib.DoCalibration();

                        return calib.CalibratedScrew;
                    }
                }
            }
        }

        /**
         * Create a new screw by setting its start and end points or adjust one of these points
         **/

        public Screw GetInOut()
        {
            GetPoint gpts = new GetPoint();
            gpts.SetCommandPrompt("Select head and tip points to position the screw. Click head point first.");
            if (headPoint == Point3d.Unset)
            {
                gpts.Constrain(headPlacementPlane, false);
            }
            else if (headPoint != Point3d.Unset && tipPoint == Point3d.Unset)
            {
                gpts.Constrain(targetMeshTip, false);
            }

            gpts.PermitObjectSnap(false);
            gpts.DynamicDraw += DrawScrew;
            gpts.AcceptNothing(true); // accept ENTER to confirm
            gpts.EnableTransparentCommands(false);
            while (true)
            {
                GetResult get_res = gpts.Get(); // function only returns after clicking
                if (get_res == GetResult.Cancel)
                {
                    return null;
                }

                if (get_res == GetResult.Point)
                {
                    // Set appropriate point

                    // When both the headPoint and tipPoint are unset
                    if (headPoint == Point3d.Unset && tipPoint == Point3d.Unset)
                    {
                        // Set headPoint
                        headPoint = gpts.Point();
                        // Switch constraining mesh to target mesh
                        gpts.Constrain(targetMeshTip, false);
                        // Continue to set the tip point
                        continue;
                    }

                    // When the headPoint is set, but the tipPoint is unset
                    if (headPoint != Point3d.Unset && tipPoint == Point3d.Unset && (gpts.Point() - headPoint).Length > 0.1)
                    {
                        // Set tipPoint
                        tipPoint = gpts.Point();
                        break;
                    }

                    // When the tipPoint is set, but the headPoint is unset
                    if (headPoint == Point3d.Unset && tipPoint != Point3d.Unset && (tipPoint - gpts.Point()).Length > 0.1)
                    {
                        headPoint = gpts.Point();
                    }

                    break;
                }
            }

            //Screw creation
            var screw = new Screw(director, headPoint, tipPoint, screwType, -1);

            ScrewCalibrator calib = new ScrewCalibrator(screw, targetMeshTip, director);
            calib.DoCalibration();

            var calibratedScrew = calib.CalibratedScrew;

            // If old screw had a set fixed length, set this fixed length in the new screw
            if (Math.Abs(fixedLength) > double.Epsilon)
            {
                calibratedScrew.FixedLength = fixedLength;
            }

            // Return the screw object
            return calibratedScrew;
        }

        /**
         * Event handler for dynamic draw event of a plane.
         */

        public void DrawScrew(Object sender, Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            Screw drawScrew = null;
            Point3d endPoint = Point3d.Unset;
            Point3d startPoint = Point3d.Unset;
            if (headPoint == Point3d.Unset && tipPoint == Point3d.Unset && PlacementMethod == ScrewPlacementMethodType.CAMERA)
            {
                // Draw from current along view (with fixed length)
                Vector3d viewDirection = director.Document.Views.ActiveView.ActiveViewport.CameraDirection;
                endPoint = e.CurrentPoint + 50 * viewDirection;
                startPoint = e.CurrentPoint;
            }
            else if (headPoint == Point3d.Unset && tipPoint != Point3d.Unset)
            {
                // Draw from head point to current
                endPoint = tipPoint;

                // Draw from current to tipPoint
                startPoint = e.CurrentPoint;
            }
            else if (headPoint != Point3d.Unset && tipPoint == Point3d.Unset)
            {
                // Draw from head point to current
                endPoint = e.CurrentPoint;
                startPoint = headPoint;
            }
            else { }

            if (startPoint != Point3d.Unset && endPoint != Point3d.Unset && (endPoint - startPoint).Length > 0.1)
            {
                // Draw the screw
                var screw = new Screw(director, startPoint, endPoint, screwType, -1);

                ScrewCalibrator calib = new ScrewCalibrator(screw, targetMeshTip, director);
                calib.DoCalibration();

                drawScrew = calib.CalibratedScrew;
                e.Display.DrawBrepShaded(drawScrew.BrepGeometry, drawScrewMaterial);
            }

            // Show screw length
            if (drawScrew != null &&
                drawScrew.HeadPoint != Point3d.Unset &&
                drawScrew.TipPoint != Point3d.Unset &&
                PlacementMethod != ScrewPlacementMethodType.CAMERA)
            {
                e.Display.DrawDot(100, 50, string.Format("{0:F0}mm", drawScrew.TotalLength), System.Drawing.Color.Black, System.Drawing.Color.White);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                drawScrewMaterial.Dispose();
            }
        }
    }
}