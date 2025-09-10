using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.Core.Drawing
{
    public abstract class GenericScrewPreview : LengthPreview, IDisposable
    {
        private readonly DisplayMaterial _material;
        protected List<Brep> ScrewComponentBreps;
        public Brep screwPreview { get; protected set; }

        public Point3d FixedPoint
        {
            get { return FromPoint; }
            set { FromPoint = value; }
        }

        public Point3d MovingPoint
        {
            get { return ToPoint; }
            set { ToPoint = value; }
        }

        protected GenericScrewPreview(Point3d fixedPoint, Point3d movingPoint, Color color, double transparency) : base(fixedPoint, movingPoint)
        {
            _material = new DisplayMaterial(color, transparency);
            ScrewComponentBreps = new List<Brep>();
        }

        public override void DrawScrew(DisplayPipeline display)
        {
            foreach (var brep in ScrewComponentBreps)
            {
                display.DrawBrepShaded(brep, _material);
            }
            base.DrawScrew(display);
        }

        protected override void OnPointChanged()
        {
            UpdateScrewComponentBreps();
        }

        protected override string GetDisplayText()
        {
            return $"{(MovingPoint - FixedPoint).Length:F0}mm";
        }

        protected abstract void UpdateScrewComponentBreps();

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