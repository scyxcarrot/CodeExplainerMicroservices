using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Linq;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Visualization;

namespace IDS.Operations.Screws
{
    /*
     * GetScrew provides functionality screw placement and screw repositioning
     */

    public class GetScrew : IDisposable
    {
        // Member variables
        private Mesh targetMeshHead;

        private Mesh targetMeshTip;
        private Point3d headPoint = Point3d.Unset;
        private Point3d tipPoint = Point3d.Unset;
        private Vector3d screwLine = Vector3d.Unset;
        private ImplantDirector director;
        private ScrewBrandType screwBrandType;
        private ScrewAlignment screwAlignment;
        private double fixedLength = 0.0;
        private double axialOffset;
        private int oldScrewIndex;
        private double originalLength = 0.0;
        public PlacementMethod placementMethod { get; set; }

        private Rhino.Display.DisplayMaterial drawScrewMaterial = new Rhino.Display.DisplayMaterial();

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
        /// <param name="oldScrew">The old screw.</param>
        public GetScrew(ImplantDirector director, PlacementMethod placementMethod, Mesh targetMeshHead, Mesh targetMeshTip, Screw oldScrew)
        {
            // director
            this.director = director;

            // placement method
            this.placementMethod = placementMethod;

            // target meshes
            this.targetMeshHead = targetMeshHead;
            this.targetMeshTip = targetMeshTip;
            this.targetMeshTip.FaceNormals.ComputeFaceNormals();

            // Get properties from the oldScrew
            headPoint = oldScrew.HeadPoint;
            tipPoint = oldScrew.TipPoint;
            if (headPoint != Point3d.Unset && tipPoint != Point3d.Unset)
            {
                originalLength = (headPoint - tipPoint).Length;
            }
            screwBrandType = oldScrew.screwBrandType;
            screwAlignment = oldScrew.screwAlignment;
            fixedLength = oldScrew.FixedLength;
            axialOffset = oldScrew.AxialOffset;
            oldScrewIndex = oldScrew.Index;

            // Calculate screw line if possible
            if (oldScrew.HeadPoint != Point3d.Unset && oldScrew.TipPoint != Point3d.Unset)
            {
                screwLine = oldScrew.TipPoint - oldScrew.HeadPoint;
            }

            // Screw material settings
            drawScrewMaterial.Ambient = Colors.MetalScrew;
            drawScrewMaterial.Specular = Colors.MetalScrew;
            drawScrewMaterial.Diffuse = Colors.MetalScrew;
            drawScrewMaterial.Transparency = 0.5;
            drawScrewMaterial.Shine = 0.5;
        }

        /**
         * Empty constructor
         **/

        public GetScrew()
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

            // Check available data and start required screw get function
            switch (placementMethod)
            {
                // Place a screw using the view direction as screw axis
                case PlacementMethod.Camera:
                    headPoint = Point3d.Unset;
                    tipPoint = Point3d.Unset;
                    theScrew = GetCamera();

                    break;

                // Place a screw by defining a head and a tip point
                case PlacementMethod.HeadTip:
                    headPoint = Point3d.Unset;
                    tipPoint = Point3d.Unset;
                    theScrew = GetInOut();
                    break;

                // Reposition the screw head
                case PlacementMethod.MoveHead:
                    if (tipPoint == Point3d.Unset)
                    {
                        return null;
                    }
                    else
                    {
                        headPoint = Point3d.Unset;
                        theScrew = GetInOut();
                    }
                    break;

                // Reposition the screw tip
                case PlacementMethod.MoveTip:
                    if (headPoint == Point3d.Unset)
                    {
                        return null;
                    }
                    else
                    {
                        tipPoint = Point3d.Unset;
                        theScrew = GetInOut();
                    }
                    break;

                // Adjust the screw length
                case PlacementMethod.AdjustLength:
                    if (headPoint == Point3d.Unset || tipPoint == Point3d.Unset)
                    {
                        return null;
                    }
                    else
                    {
                        theScrew = SetLength();
                    }
                    break;

                // Translate the screw using a newly defined head point
                case PlacementMethod.Translate:
                    if (headPoint == Point3d.Unset || tipPoint == Point3d.Unset)
                    {
                        return null;
                    }
                    else
                    {
                        headPoint = Point3d.Unset;
                        theScrew = Translate();
                    }
                    break;

                default:
                    return null;
            }

            // Set the return screw index to the old one
            if (null != theScrew)
            {
                theScrew.Index = oldScrewIndex;
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
            gpts.Constrain(targetMeshHead, false);
            gpts.PermitObjectSnap(false);
            gpts.DynamicDraw += DrawScrew;
            gpts.AcceptNothing(true); // accept ENTER to confirm
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

                    // Find appropriate tip point
                    double dist = 40.0;

                    // Calculate screw tip point with minimum length
                    double minScrewLength = Screw.GetScrewLengths(screwBrandType).Min();
                    if (dist <= minScrewLength)
                    {
                        tipPoint = headPoint + rayDirection * minScrewLength;
                    }
                    else
                    {
                        tipPoint = headPoint + rayDirection * dist;
                    }

                    break;
                }
            }
            return new Screw(director, headPoint, tipPoint, screwBrandType, screwAlignment);
        }

        /**
         * Create a new screw by setting its start and end points or adjust one of these points
         **/

        public Screw GetInOut()
        {
            GetPoint gpts = new GetPoint();
            gpts.SetCommandPrompt("Select head and tip points to position the screw. Click head point first.");
            if (headPoint == Point3d.Unset && tipPoint == Point3d.Unset)
            {
                gpts.Constrain(targetMeshHead, false);
            }
            else if (headPoint != Point3d.Unset && tipPoint == Point3d.Unset)
            {
                gpts.Constrain(targetMeshTip, false);
            }
            else if (headPoint == Point3d.Unset && tipPoint != Point3d.Unset)
            {
                gpts.Constrain(targetMeshHead, false);
            }
            gpts.PermitObjectSnap(false);
            gpts.DynamicDraw += DrawScrew;
            gpts.AcceptNothing(true); // accept ENTER to confirm
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
                    if (headPoint != Point3d.Unset && tipPoint == Point3d.Unset)
                    {
                        // Set tipPoint
                        if ((gpts.Point() - headPoint).Length > 0.1)
                        {
                            tipPoint = gpts.Point();
                            break;
                        }
                    }
                    // When the tipPoint is set, but the headPoint is unset
                    else if (headPoint == Point3d.Unset && tipPoint != Point3d.Unset)
                    {
                        // Set headPoint
                        if ((tipPoint - gpts.Point()).Length > 0.1)
                        {
                            headPoint = gpts.Point();
                            break;
                        }
                    }
                    else
                    {
                        // Just in case...
                        break;
                    }
                }
            }

            // Screw can be created with the new parameters
            Screw newScrew = new Screw(director, headPoint, tipPoint, screwBrandType, screwAlignment);

            // If old screw had a set fixed length, set this fixed length in the new screw
            if (Math.Abs(fixedLength) > 0.00001)
            {
                newScrew.FixedLength = fixedLength;
            }

            // Return the screw object
            return newScrew;
        }

        /**
         * Move the screw parallel to its own axis, while redefining the head
         **/

        public Screw Translate()
        {
            GetPoint gpts = new GetPoint();
            gpts.SetCommandPrompt("Select new screw head.");
            gpts.Constrain(targetMeshHead, false);
            gpts.PermitObjectSnap(false);
            gpts.DynamicDraw += DrawScrew;
            gpts.AcceptNothing(true); // accept ENTER to confirm
            while (true)
            {
                GetResult get_res = gpts.Get(); // function only returns after clicking
                if (get_res == GetResult.Cancel)
                {
                    return null;
                }

                if (get_res == GetResult.Point)
                {
                    // Set headpoint
                    headPoint = gpts.Point();
                    // Adjust tippoint to the nearest intersection with the _targetMeshTip
                    tipPoint = headPoint + screwLine;
                    Vector3d direction = (tipPoint - headPoint);
                    direction.Unitize();
                    // Do ray intersection
                    Ray3d rayForwards = new Ray3d(tipPoint, direction);
                    Ray3d rayBackwards = new Ray3d(tipPoint, -direction);
                    double intersectionForwards = Intersection.MeshRay(targetMeshTip, rayForwards);
                    double intersectionBackwards = Intersection.MeshRay(targetMeshTip, rayBackwards);
                    if (Math.Abs(intersectionForwards - (-1)) < 0.0001)
                    {
                        tipPoint -= direction * intersectionBackwards;
                    }
                    else if (Math.Abs(intersectionBackwards - (-1)) < 0.0001)
                    {
                        tipPoint += direction * intersectionForwards;
                    }
                    else if (intersectionForwards < intersectionBackwards)
                    {
                        tipPoint += direction * intersectionForwards;
                    }
                    else
                    {
                        tipPoint -= direction * intersectionBackwards;
                    }
                    break;
                }
            }

            // Create new screw
            Screw newScrew = new Screw(director, headPoint, tipPoint, screwBrandType, screwAlignment);

            // If old screw had a set fixed length, set this fixed length in the new screw
            if (Math.Abs(fixedLength) > 0.00001)
            {
                newScrew.FixedLength = fixedLength;
            }

            // Return the screw
            return newScrew;
        }

        /**
         * Adjust the length of an existing screw
         **/

        public Screw SetLength()
        {
            // init
            Screw screw;

            // Verify that all data is available
            if (headPoint == Point3d.Unset || tipPoint == Point3d.Unset)
            {
                return null;
            }

            // Set axis to move along
            double minScrewLength = Screw.GetScrewLengths(screwBrandType).Min();
            double maxScrewLength = Screw.GetScrewLengths(screwBrandType).Max();
            Vector3d axis = new Vector3d(tipPoint - headPoint);
            axis.Unitize();
            LineCurve scrLine = new LineCurve(headPoint + axis * minScrewLength, headPoint + axis * maxScrewLength);

            // Unset the tip point
            tipPoint = Point3d.Unset;

            GetPoint gpts = new GetPoint();
            gpts.SetCommandPrompt("Set the screw length.");
            gpts.Constrain(scrLine.ToNurbsCurve(), false);
            gpts.PermitObjectSnap(false);
            gpts.DynamicDraw += DrawScrew;
            gpts.AcceptNumber(true, false);
            gpts.AcceptNothing(true); // accept ENTER to confirm
            while (true)
            {
                GetResult get_res = gpts.Get();
                if (get_res == GetResult.Cancel)
                {
                    return null;
                }

                if (get_res == GetResult.Nothing)
                {
                    // Just return a screw with the original properties, except for _fixedlength
                    // being 0.0 and a dummy tip at 10mm length The update of the screw will
                    // calculate the bicorticality and adapt the screw tip
                    Rhino.RhinoApp.WriteLine("Screw was made bicortical");
                    tipPoint = headPoint + axis * 10.0;
                    screw = new Screw(director, headPoint, tipPoint, screwBrandType, screwAlignment);
                    return screw;
                }

                if (get_res == GetResult.Point)
                {
                    // Set tip point
                    tipPoint = gpts.Point();
                    break;
                }

                if (get_res == GetResult.Number)
                {
                    int length = (int)gpts.Number();
                    if (length > Screw.GetScrewLengths(screwBrandType).Min())
                    {
                        tipPoint = headPoint + axis * length;
                        break;
                    }
                }
            }

            // Adjust tip according to available screw lengths
            screw = new Screw(director, headPoint, tipPoint, screwBrandType, screwAlignment, 0, axialOffset);

            // fix the length
            screw.SetAvailableLength();
            screw.FixedLength = screw.GetAvailableLength();

            return screw;
        }

        /**
         * Event handler for dynamic draw event of a plane.
         */

        public void DrawScrew(Object sender, Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            Screw drawScrew = null;
            Point3d endPoint = Point3d.Unset;
            Point3d startPoint = Point3d.Unset;
            if (headPoint == Point3d.Unset && tipPoint == Point3d.Unset && placementMethod == PlacementMethod.Camera)
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
                // Show fixed length if it was set by the user
                if (placementMethod == PlacementMethod.Translate)
                {
                    endPoint = e.CurrentPoint + screwLine;
                }

                // Draw from current to tipPoint
                startPoint = e.CurrentPoint;
            }
            else if (headPoint != Point3d.Unset && tipPoint == Point3d.Unset)
            {
                // Draw from head point to current
                endPoint = e.CurrentPoint;
                // Set fixed length if it was set by the user
                if (Math.Abs(fixedLength) > 0.00001 && placementMethod != PlacementMethod.AdjustLength)
                {
                    Vector3d axis = (e.CurrentPoint - headPoint);
                    axis.Unitize();
                    endPoint = headPoint + axis * fixedLength;
                }
                startPoint = headPoint;
            }

            if (startPoint != Point3d.Unset && endPoint != Point3d.Unset && (endPoint - startPoint).Length > 0.1)
            {
                // Round to mm
                Screw unroundedScrew = new Screw(director, startPoint, endPoint, screwBrandType, ScrewAlignment.Invalid, 0);
                double currentLength = (endPoint - startPoint).Length;
                endPoint = endPoint - unroundedScrew.Direction * (currentLength - unroundedScrew.GetAvailableLength());
                // Draw the screw
                drawScrew = new Screw(director, startPoint, endPoint, screwBrandType, ScrewAlignment.Invalid, 0);
                e.Display.DrawBrepShaded(drawScrew.BrepGeometry, drawScrewMaterial);
            }

            // Show screw length
            if (drawScrew != null &&
                drawScrew.HeadPoint != Point3d.Unset &&
                drawScrew.TipPoint != Point3d.Unset &&
                placementMethod != PlacementMethod.Camera &&
                placementMethod != PlacementMethod.Translate)
            {
                if (placementMethod == PlacementMethod.AdjustLength ||
                    placementMethod == PlacementMethod.MoveHead ||
                    placementMethod == PlacementMethod.MoveTip)
                {
                    e.Display.DrawDot(100, 50,
                        string.Format("{0:F0}mm (was {1:F0}mm)", drawScrew.GetAvailableLength(), originalLength),
                        System.Drawing.Color.Black, System.Drawing.Color.White);
                }
                else
                {
                    e.Display.DrawDot(100, 50, string.Format("{0:F0}mm", drawScrew.GetAvailableLength()), System.Drawing.Color.Black, System.Drawing.Color.White);
                }
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