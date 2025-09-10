using IDS.Core.Drawing;
using IDS.Core.Utilities;
using IDS.Glenius.Visualization;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class DrawPlate : DrawCurve
    {
        private readonly PlateDrawingPlaneConduit conduit;
        private BasePlatePreview preview;
        private List<Point3d> cachedPointList;
        private bool drawTop = true;

        public DrawPlate(GleniusImplantDirector director) : base(director.Document)
        {
            conduit = new PlateDrawingPlaneConduit(director);
        }

        public Curve DrawTop(Plane topPlane)
        {
            var cylOutlineOffset = conduit.CylinderOutlineOffset.DuplicateCurve().Rebuild(16, 2, true);

            var rotationAxis = topPlane.ZAxis;
            var rotationCenter = CurveUtilities.GetCurveCentroid(cylOutlineOffset);
            cylOutlineOffset.Rotate(Rhino.RhinoMath.ToRadians(5), rotationAxis, rotationCenter);

            return EditTop(cylOutlineOffset, topPlane);
        }

        public Curve EditTop(Curve baseCurve, Plane topPlane)
        {
            conduit.DisplayPlane = topPlane;
            conduit.DrawOutlines = true;
            conduit.Enabled = true;

            drawTop = true;
            AcceptNothing(true);
            var cylOutline = conduit.CylinderOutline.DuplicateCurve().Rebuild(16, 2, true);
            var cylOutlineOffset = conduit.CylinderOutlineOffset.DuplicateCurve().Rebuild(16, 2, true);

            SetExistingCurve(baseCurve, true, false);
            
            //Snap Curves
            SnapCurves = new List<Curve>() { cylOutline, cylOutlineOffset };

            if (SetConstraintPlane(topPlane, new Interval(-100, 100), false, new Curve[] { cylOutline }))
            {
                var curve = Draw();
                conduit.Enabled = false;
                return curve;
            }
            conduit.Enabled = false;
            return null;
        }

        public Curve DrawBottom(Curve topCurve, Curve bottomCurve, Plane bottomPlane)
        {
            conduit.DisplayPlane = bottomPlane;
            conduit.DrawOutlines = false;
            conduit.Enabled = true;

            drawTop = false;

            preview = new BasePlatePreview();
            preview.ShowSideWallOnly = true;
            preview.TopCurve = topCurve;
            preview.BottomCurve = bottomCurve;

            var size = 100;
            AcceptNothing(true);
            SetExistingCurve(bottomCurve, true, false);
            SetConstraintPlane(bottomPlane, new Interval(-size, size), false, false);
            cachedPointList = PointList.ToList();
            var newBottomCurve = Draw();
            
            preview = null;
            conduit.Enabled = false;
            return newBottomCurve;
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e); // Do all the DrawCurve drawing

            if (!drawTop && preview != null)
            {
                if (!cachedPointList.SequenceEqual(PointList))
                {
                    cachedPointList = PointList.ToList();
                    var bottom = BuildCurve(true);
                    preview.BottomCurve = bottom;
                }
                preview.DrawPreview(e.Display);
            }
        }
    }
}
