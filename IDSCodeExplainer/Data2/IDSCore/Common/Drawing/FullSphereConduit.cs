using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace IDS.Core.Drawing
{
    public class FullSphereConduit : DisplayConduit, IDisposable
    {
        public const double DefaultTransparency = 0.5;

        private Sphere _sphere;
        private Brep _sphereBrep;
        private readonly DisplayMaterial _material;

        private Point3d _center;
        public Point3d Center
        {
            get { return _center; }
            set
            {
                _center = value;
                _sphere.Center = _center;
                _sphereBrep = _sphere.ToBrep();
            }
        }

        private double _diameter;
        public double Diameter
        {
            get { return _diameter; }
            set
            {
                _diameter = value;
                _sphere.Radius = _diameter / 2;
                _sphereBrep = _sphere.ToBrep();
            }
        }

        private double _transparency;
        public double Transparency
        {
            get { return _transparency; }
            set
            {
                _transparency = value;
                _material.Transparency = _transparency;
            }
        }

        public FullSphereConduit(Point3d center, double diameter)
            : this(center, diameter, DefaultTransparency)
        {

        }

        public FullSphereConduit(Point3d center, double diameter, double transparency)
            : this(center, diameter, transparency, Color.White)
        {

        }

        public FullSphereConduit(Point3d center, double diameter, double transparency, Color color)
        {
            _center = center;
            _diameter = diameter;
            _transparency = transparency;
            _sphere = new Sphere(_center, _diameter / 2);
            _sphereBrep = _sphere.ToBrep();
            _material = new DisplayMaterial(color, _transparency);
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);            
            e.Display.DrawBrepShaded(_sphereBrep, _material);
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            e.IncludeBoundingBox(_sphere.BoundingBox);
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