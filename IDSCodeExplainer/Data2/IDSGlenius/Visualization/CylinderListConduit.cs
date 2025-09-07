using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.Glenius.Visualization
{
    public class CylinderListConduit : DisplayConduit, IDisposable
    {
        private readonly List<Brep> _cylinderBreps;
        private readonly DisplayMaterial _material;
        private BoundingBox _bbox;

        public CylinderListConduit(List<Cylinder> cylinderList)
        {
            _cylinderBreps = new List<Brep>();
            _bbox = BoundingBox.Empty;
            foreach (var cylinder in cylinderList)
            {
                var cylinderBrep = cylinder.ToBrep(true, true);
                _cylinderBreps.Add(cylinderBrep);
                _bbox.Union(cylinderBrep.GetBoundingBox(true));
            }
            _material = new DisplayMaterial(Color.White, 0.5);
        }

        public void UpdateMaterial(double transparency, Color color)
        {
            if (Math.Abs(_material.Transparency - transparency) > 0.001)
            {
                _material.Transparency = transparency;
            }

            if (_material.Diffuse != color)
            {
                _material.Diffuse = color;
                _material.Specular = color;
                _material.Ambient = color;
                _material.Emission = color;
            }
        }
        
        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);
            foreach (var cylinder in _cylinderBreps)
            {
                e.Display.DrawBrepShaded(cylinder, _material);
            }
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            e.IncludeBoundingBox(_bbox);
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