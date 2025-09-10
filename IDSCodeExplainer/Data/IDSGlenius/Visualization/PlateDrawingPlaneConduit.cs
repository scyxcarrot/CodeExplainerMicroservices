using IDS.Core.Utilities;
using IDS.Glenius.Constants;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.Glenius.Visualization
{
    public class PlateDrawingPlaneConduit : DisplayConduit, IDisposable
    {
        private readonly double _plateOffset;

        private readonly GleniusObjectManager _objectManager;
        private readonly Plane _planeCoordinateSystem;
        private BoundingBox _boundingBox;
        private BoundingBox _refBoundingBox;
        private Brep _planeBrep;
        private readonly DisplayMaterial _material;
        private readonly Color _outlineColor;

        public Curve CylinderOutline { get; private set; }

        public Curve CylinderOutlineOffset { get; private set; }

        public List<Curve> ScrewMantleOutlines { get; private set; }

        public Plane DisplayPlane
        {
            set { UpdatePlaneBrep(value); }
        }

        public bool DrawOutlines { get; set; }

        public PlateDrawingPlaneConduit(GleniusImplantDirector director)
        {
            _objectManager = new GleniusObjectManager(director);
            _boundingBox = BoundingBox.Empty;
            _material = new DisplayMaterial(Color.Gray, Transparency.Low);
            _outlineColor = Color.Black;

            var plateDrawingGenerator = new PlateDrawingPlaneGenerator(director);
            _planeCoordinateSystem = plateDrawingGenerator.GenerateTopPlane();
             
            _plateOffset = 1.0;
            CreatePlaneAndOutlines();
            DrawOutlines = true;
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            e.Display.DrawBrepShaded(_planeBrep, _material);

            if (!DrawOutlines)
            {
                return;
            }
            e.Display.DrawCurve(CylinderOutline, _outlineColor);
            e.Display.DrawCurve(CylinderOutlineOffset, _outlineColor);

            foreach (var outline in ScrewMantleOutlines)
            {
                e.Display.DrawCurve(outline, _outlineColor);
            }
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            e.IncludeBoundingBox(_boundingBox);
        }

        private void CreatePlaneAndOutlines()
        {
            const double offset = 0.1; //use an offset to create a cleaner intersect
            var originOffset = Point3d.Add(_planeCoordinateSystem.Origin, Vector3d.Multiply(_planeCoordinateSystem.ZAxis, -offset));
            var planeWithOffset = new Plane(originOffset, _planeCoordinateSystem.ZAxis);
            var cylinderHatMesh = MeshUtilities.ConvertBrepToMesh(_objectManager.GetBuildingBlock(IBB.CylinderHat).Geometry as Brep);
            var cylinderOutlines = Intersection.MeshPlane(cylinderHatMesh, planeWithOffset);
            if (cylinderOutlines != null && cylinderOutlines.Length == 1)
            {
                var polyline = cylinderOutlines[0].ConvertAll(point => Point3d.Add(point, Vector3d.Multiply(_planeCoordinateSystem.ZAxis, offset)));
                CylinderOutline = Curve.CreateControlPointCurve(polyline);

                var polylineWithOffset = polyline.ConvertAll((point) =>
                {
                    var direction = point - _planeCoordinateSystem.Origin;
                    if (!direction.IsUnitVector)
                    {
                        direction.Unitize();
                    }
                    return Point3d.Add(point, Vector3d.Multiply(direction, _plateOffset));
                });
                CylinderOutlineOffset = Curve.CreateControlPointCurve(polylineWithOffset);
            }

            ScrewMantleOutlines = new List<Curve>();
            var overallBox = cylinderHatMesh.GetBoundingBox(true);
            var plane = new Plane(_planeCoordinateSystem.Origin, _planeCoordinateSystem.ZAxis);
            var screwMantles = _objectManager.GetAllBuildingBlocks(IBB.ScrewMantle).Select(mantle => MeshUtilities.ConvertBrepToMesh(mantle.Geometry as Brep));
            foreach (var mantle in screwMantles)
            {
                var outlines = Intersection.MeshPlane(mantle, plane);
                if (outlines == null || outlines.Length != 1)
                {
                    continue;
                }

                ScrewMantleOutlines.Add(outlines[0].ToNurbsCurve());
                overallBox.Union(mantle.GetBoundingBox(true));
            }

            var min = plane.ClosestPoint(overallBox.Min);
            var max = plane.ClosestPoint(overallBox.Max);
            var length = (max - min).Length;
            overallBox.Inflate(length);
            _refBoundingBox = overallBox;
            UpdatePlaneBrep(plane);
        }

        private void UpdatePlaneBrep(Plane plane)
        {
            var planesurface = PlaneSurface.CreateThroughBox(plane, _refBoundingBox);
            _planeBrep = planesurface.ToBrep();
            _boundingBox = _planeBrep.GetBoundingBox(true);
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
                _material.Dispose();
            }
        }
    }
}