using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using System;
using System.Linq;

namespace IDS.CMF.Factory
{
    public class ScrewBrepFactory
    {
        private readonly Brep screwHead = null;
        public static readonly Vector3d ScrewAxis = -Vector3d.ZAxis;
        public static readonly Point3d ScrewHeadPointAtOrigin = Plane.WorldXY.Origin;

        public ScrewBrepFactory(Brep screwHeadBrep)
        {
            screwHead = screwHeadBrep;
        }

        public double GetHeadHeight()
        {
            var naked_curves = screwHead.DuplicateEdgeCurves(true);
            if (naked_curves.Length == 0)
            {
                throw new Core.PluginHelper.IDSException("No open edges found!");
            }
            var minCtrlPts = naked_curves.SelectMany(p => CurveUtilities.GetCurveControlPoints(p).Select(q => q.Z)).OrderBy(r=>r);
            return 0.0 - minCtrlPts.First();
        }

        public double GetHeadCenterOffsetFromHeadPoint()
        {
            var headHeight = GetHeadHeight();
            var offset = headHeight / 2;
            return -offset;
        }

        public static Transform GetAlignmentTransform(Vector3d orientation, Point3d headPoint)
        {
            Transform rotation = Transform.Rotation(-Plane.WorldXY.ZAxis, orientation, ScrewHeadPointAtOrigin);
            Transform translation = Transform.Translation(headPoint - ScrewHeadPointAtOrigin);
            Transform fullTransform = Transform.Multiply(translation, rotation);
            return fullTransform;
        }

        public double GetScrewBodyRadius()
        {
            var naked_curves = screwHead.DuplicateEdgeCurves(true);
            if (naked_curves.Length == 0)
            {
                throw new Core.PluginHelper.IDSException("No open edges found!");
            }
            //get screw head body radius in Y axis
            var minCtrlPts = naked_curves.SelectMany(p => CurveUtilities.GetCurveControlPoints(p).Select(q => q.Y)).OrderBy(r => r);
            var diameter = Math.Abs(minCtrlPts.Last() - minCtrlPts.First());
            return diameter / 2;

        }

        public Brep CreateScrewBrep(Point3d headPoint, Point3d tipPoint)
        {
            Vector3d orientation = tipPoint - headPoint;
            orientation.Unitize();

            //Screw are created in the origin of WCS, where -Z Axis is the screw direction.
            Point3d headCenter = new Point3d(0, 0, GetHeadCenterOffsetFromHeadPoint());
            double bodyRadius = GetScrewBodyRadius();
            Point3d bodyOriginOffsetted = new Point3d(0, 0, -GetHeadHeight());
            Point3d bodyStart = bodyOriginOffsetted + (Vector3d.XAxis * bodyRadius);

            //Get closest number
            double bodyLength = (headPoint - tipPoint).Length;
            bodyLength = bodyLength - GetHeadHeight(); //minus off the head height, since body length starts from Head

            // Create full contour and add head contour as first part
            PolyCurve fullContour = new PolyCurve();

            // Make part of contour representing the body
            Point3d bodyLineEnd = bodyStart + (ScrewAxis * (bodyLength - bodyRadius));
            Line bodyLine = new Line(bodyStart, bodyLineEnd);
            fullContour.Append(bodyLine);

            // Make part of contour representing the tip
            Point3d tipEnd = bodyOriginOffsetted + (ScrewAxis * bodyLength);
            Line tipLine = new Line(bodyLineEnd, tipEnd);
            fullContour.Append(tipLine);

            // Create revolution surface (closed surface = solid)
            Line revAxis = new Line(headCenter, tipEnd);
            RevSurface revSurf = RevSurface.Create(fullContour, revAxis);
            var solidBody = Brep.CreateFromRevSurface(revSurf, false, false);

            var tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            var solidHead = screwHead.DuplicateBrep();
            solidHead = solidHead.CapPlanarHoles(tolerance);
            if (solidHead.SolidOrientation == BrepSolidOrientation.Inward)
            {
                solidHead.Flip();
            }

            // Transform to align with screw
            var solidScrew = Brep.CreateBooleanUnion(new[] { solidHead, solidBody }, tolerance)[0];

            //Calibrate the placement
            solidScrew.Transform(GetAlignmentTransform(orientation, headPoint));

            return solidScrew;
        }
    }
}
