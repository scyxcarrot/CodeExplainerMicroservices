using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;


namespace IDS.Amace.ImplantBuildingBlocks
{
    public static class ScrewHelper
    {
        /// <summary>
        /// The suffix head contour
        /// </summary>
        private const string SuffixHeadContour = "_CONTOUR_HEAD";

        /// <summary>
        /// Gets the screw head contour.
        /// </summary>
        /// <param name="screwDatabase">The screw database.</param>
        /// <param name="screwType">Type of the screw.</param>
        /// <returns></returns>
        public static Curve GetScrewHeadContour(File3dm screwDatabase, string screwTypeString)
        {
            var curveTag = screwTypeString + SuffixHeadContour;
            var target = screwDatabase.Objects.FirstOrDefault(rhobj => string.Equals(rhobj.Attributes.Name, curveTag, StringComparison.InvariantCultureIgnoreCase));
            return target?.Geometry as Curve;
        }

        /// <summary>
        /// Computes the brep.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="screwDatabase"></param>
        /// <param name="headPoint">The head point.</param>
        /// <param name="tipPoint">The tip point.</param>
        /// <param name="screwType">Type of the screw.</param>
        /// <param name="screwTypeString"></param>
        /// <returns></returns>
        public static Brep ComputeBrep(File3dm screwDatabase, Point3d headPoint, Point3d tipPoint, string screwTypeString)
        {
            // Get head contour
            var headContour = GetScrewHeadContour(screwDatabase, screwTypeString);

            // Screw parameters
            if (headContour.PointAtStart != Point3d.Origin)
            {
                headContour.Reverse();
            }

            var orientation = tipPoint - headPoint;
            var totalLength = orientation.Length;
            orientation.Unitize();
            var headLength = Math.Abs(headContour.PointAtStart.Z - headContour.PointAtEnd.Z);
            var bodyRadius = Math.Abs(headContour.PointAtEnd.X);
            var bodyLength = totalLength - headLength;

            // Create screw Brep from contour and revolve
            var screwAxis = -Vector3d.ZAxis;
            var headOrigin = headContour.PointAtStart;
            var bodyStart = headContour.PointAtEnd;
            var bodyOrigin = bodyStart;
            bodyOrigin.X = 0.0; // highest center point of screw body

            // Create full contour and add head contour as first part
            var fullContour = new PolyCurve();
            fullContour.Append(headContour);
            fullContour.RemoveNesting(); // In case contour already was polycurve

            // Make part of contour representing the body
            var bodyLineEnd = bodyStart + (screwAxis * (bodyLength - bodyRadius));
            var bodyLine = new Line(bodyStart, bodyLineEnd);
            fullContour.Append(bodyLine);

            // Make part of contour representing the tip
            var tipEnd = bodyOrigin + (screwAxis * bodyLength);
            var tipLine = new Line(bodyLineEnd, tipEnd);
            fullContour.Append(tipLine);

            // Create revolution surface (closed surface = solid)
            var revAxis = new Line(headOrigin, tipEnd);
            var revSurf = RevSurface.Create(fullContour, revAxis);
            var solidScrew = Brep.CreateFromRevSurface(revSurf, true, true);

            // Subtract hexagon from head
            var hexaPoints = new List<Point3d>();
            const double r = 1.75; // temp
            var rd = Math.Sqrt(3) / 2 * r;
            const double depth = -1.5;
            hexaPoints.Add(new Point3d(r / 2, rd, depth));
            hexaPoints.Add(new Point3d(r, 0, depth));
            hexaPoints.Add(new Point3d(r / 2, -rd, depth));
            hexaPoints.Add(new Point3d(-r / 2, -rd, depth));
            hexaPoints.Add(new Point3d(-r, 0, depth));
            hexaPoints.Add(new Point3d(-r / 2, rd, depth));
            hexaPoints.Add(new Point3d(r / 2, rd, depth));
            var hexaLine = new Polyline(hexaPoints);
            var hexagon = Surface.CreateExtrusion(hexaLine.ToNurbsCurve(), (-depth + 1) * (-screwAxis)).ToBrep().CapPlanarHoles(0.01);
            hexagon.Flip();

            try
            {
                // Subtract screw head hexagon from screw
                solidScrew = Brep.CreateBooleanDifference(solidScrew, hexagon, 0.01)[0];
            }
            catch
            {
                // Do not subtract screw head hexagon, this is sometimes impossible in dynamic screw drawing
            }

            // Transform to align with screw
            solidScrew.Transform(GetAlignmentTransform(orientation, headPoint));

            return solidScrew;
        }

        /// <summary>
        /// Creates from archived.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <param name="replaceInDoc">if set to <c>true</c> [replace in document].</param>
        /// <returns></returns>
        public static Screw CreateFromArchived(RhinoObject other, bool replaceInDoc)
        {
            // Restore the screw object from archive
            var restored = new Screw(other, true, true);

            // Replace if necessary
            if (!replaceInDoc)
            {
                return restored;
            }

            var replaced = IDSPluginHelper.ReplaceRhinoObject(other, restored);
            return !replaced ? null : restored;
        }

        /// <summary>
        /// Gets the alignment transform.
        /// </summary>
        /// <param name="orientation">The orientation.</param>
        /// <param name="headPoint">The head point.</param>
        /// <returns></returns>
        public static Transform GetAlignmentTransform(Vector3d orientation, Point3d headPoint)
        {
            var rotation = Transform.Rotation(-Plane.WorldXY.ZAxis, orientation, Plane.WorldXY.Origin);
            var translation = Transform.Translation(headPoint - Plane.WorldXY.Origin);
            var fullTransform = Transform.Multiply(translation, rotation);
            return fullTransform;
        }

        /// <summary>
        /// Gets the screw lengths.
        /// </summary>
        /// <param name="screwType">Type of the screw.</param>
        /// <returns></returns>
        public static double[] GetScrewLengths()
        {
            return MathUtilities.Range(10.0, 200.0, 1.0).ToArray(); // independent of screw type
        }
    }
}
