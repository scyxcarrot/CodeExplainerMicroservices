using IDS.Amace.ImplantBuildingBlocks;
using IDS.Common.Visualisation;
using IDS.Core.Drawing;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Common.Visualization
{
    public class DrawPlate : DrawCurve
    {
        private bool drawTop = true;
        private RhinoDoc document;
        private IPlateConduitProperties _plateConduitProperties;
        private List<Tuple<Line, double>> _sideLines = null;
        private PlateConduit conduit;
        private ObjectManager _objectManager;

        public DrawPlate(RhinoDoc document, IPlateConduitProperties plateConduitProperties, ObjectManager objectManager) : base(document)
        {
            this.document = document;
            _plateConduitProperties = plateConduitProperties;
            _objectManager = objectManager;
        }

        public Curve DrawTop()
        {
            // Drawing the top curve
            drawTop = true;
            // Snap to screw outlines
            base.EnableSnapToCurves(true);

            return Draw();
        }

        public Curve DrawBottom()
        {
            // Drawing the bottom curve
            drawTop = false;
            
            // Enable the conduit to show the plate sides
            conduit = new PlateConduit(_plateConduitProperties);

            return Draw();
        }

        private Curve Draw()
        {
            // Enable the conduit to show the plate sides
            conduit = new PlateConduit(_plateConduitProperties);
            conduit.Enabled = true;
            // Start drawing
            Curve res = base.Draw();
            // Disable the conduit when the user is done
            conduit.Enabled = false;

            return res;
        }

        public void SetSidePreview(NurbsCurve curveTop, NurbsCurve curveBottom)
        {
            if (!base.CurveUpdated)
            {
                return;
            }

            // Attempt to do a sweep
            try
            {
                // Calculate sidelines and add them to the conduit
                if (!drawTop && curveTop != null && curveBottom != null)
                {
                    _sideLines = new List<Tuple<Line, double>>();
                    for (double t0 = 0.0; t0 <= 1.0; t0 += 0.001)
                    {
                        Point3d from = curveTop.PointAtNormalizedLength(t0);
                        double t1 = 0.0;
                        curveBottom.ClosestPoint(from, out t1);
                        Point3d to = curveBottom.PointAt(t1);
                        _sideLines.Add(new Tuple<Line, double>(new Line(from, to), 90 - MathUtilities.SharpCornerAngle((from - to).Length, _plateConduitProperties.PlateThickness)));
                    }

                    conduit.AngleLines = _sideLines;
                    conduit.DrawLines = true;
                }
                
                conduit.DrawDots = false;
                conduit.DrawColors = false;
                conduit.OppositeCurve = drawTop ? curveBottom : curveTop;
                conduit.DrawReferenceObjects = true;
                // Trick conduit into redrawing
                document.Views.ActiveView.ActiveViewport.SetCameraLocations(document.Views.ActiveView.ActiveViewport.CameraTarget, document.Views.ActiveView.ActiveViewport.CameraLocation);

            }
            catch
            {
                // Don't draw if sweep fails
            }

            base.CurveUpdated = false;
        }

        /*
         * The OnDynamicDraw event uses a conduit to draw the actual preview, since conduits
         * support depth culling in the PostDrawObjects event
         */

        protected override void OnDynamicDraw(Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e); // Do all the DrawCurve drawing

            NurbsCurve curveTop = null;
            NurbsCurve curveBottom = null;

            if (_sideLines == null || _sideLines.Count == 0) // First draw
            {
                if (_objectManager.HasBuildingBlock(BuildingBlocks.Blocks[IBB.PlateContourTop]))
                {
                    curveTop = _objectManager.GetBuildingBlock(BuildingBlocks.Blocks[IBB.PlateContourTop]).Geometry as NurbsCurve;
                }
                if (_objectManager.HasBuildingBlock(BuildingBlocks.Blocks[IBB.PlateContourBottom]))
                {
                    curveBottom = _objectManager.GetBuildingBlock(BuildingBlocks.Blocks[IBB.PlateContourBottom]).Geometry as NurbsCurve;
                }
            }
            else
            {
                if (drawTop && existingCurve != null) // user is editing top curve
                {
                    curveTop = BuildCurve(true).ToNurbsCurve();
                    curveBottom = _objectManager.GetBuildingBlock(BuildingBlocks.Blocks[IBB.PlateContourBottom]).Geometry as NurbsCurve;
                }
                else if (!drawTop && existingCurve != null) // user is editing bottom curve
                {
                    curveTop = _objectManager.GetBuildingBlock(BuildingBlocks.Blocks[IBB.PlateContourTop]).Geometry as NurbsCurve;
                    curveBottom = BuildCurve(true).ToNurbsCurve();
                }
            }

            SetSidePreview(curveTop, curveBottom);
        }
    }
}