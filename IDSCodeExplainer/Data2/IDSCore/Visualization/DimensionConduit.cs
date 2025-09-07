using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace IDS.Core.Visualization
{
    public class DimensionConduit : DisplayConduit, IDisposable
    {
        private readonly Dimension _dimension;
        private readonly Mesh _sph1Mesh = null;
        private readonly Mesh _sph2Mesh = null;
        private readonly DisplayMaterial _sphColor = new DisplayMaterial(Color.LawnGreen);
        public static double SphereRadius { get; set; } = 0.01;
        private const int Resolution = 50;

        public ObjRef RhObjPair { get; }
        public Color DisplayColor { get; set; }


        public bool IsVisible { get; set; }

        public DimensionConduit(Dimension dimension, ObjRef rhObjPair)
        {
            _dimension = dimension;
            RhObjPair = rhObjPair;
            IsVisible = true;
            SetToDefaultDisplayColor();
            RhinoDoc.LayerTableEvent += RhinoDoc_LayerTableEvent;

            if (SphereRadius <= 0)
            {
                return;
            }

            var linearDimension = _dimension as LinearDimension;

            if (linearDimension != null)
            {
                Point3d extensionLine1End;
                Point3d extensionLine2End;
                Point3d arrowhead1End;
                Point3d arrowhead2End;
                Point3d dimlinepoint;
                Point3d textpoint;
                if (linearDimension.Get3dPoints(out extensionLine1End,
                    out extensionLine2End, out arrowhead1End, out arrowhead2End, out dimlinepoint, out textpoint))
                {
                    var sph1 = new Sphere(arrowhead1End, SphereRadius);
                    var sph2 = new Sphere(arrowhead2End, SphereRadius);

                    _sph1Mesh = Mesh.CreateFromSphere(sph1, Resolution, Resolution);
                    _sph2Mesh = Mesh.CreateFromSphere(sph2, Resolution, Resolution);
                }
            }
        }

        public void SetToDefaultDisplayColor()
        {
            DisplayColor = Color.Black;
        }

        private void RhinoDoc_LayerTableEvent(object sender, LayerTableEventArgs e)
        {
            var obj = e.Document.Objects.Find(RhObjPair.ObjectId);

            if (obj == null) //object got deleted.
            {
                return;
            }

            var dimLayerIdx = obj.Attributes.LayerIndex;

            if (dimLayerIdx == e.LayerIndex)
            {

                IsVisible = e.NewState.IsVisible;
            }
        }

        protected override void DrawOverlay(DrawEventArgs e)
        {
            base.DrawOverlay(e);

            if (IsVisible)
            {
                e.Display.DrawAnnotation(_dimension, DisplayColor);
            }
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.DrawOverlay(e);

            if (IsVisible)
            {
                if (_sph1Mesh != null)
                {
                    e.Display.DrawMeshShaded(_sph1Mesh, _sphColor);
                }

                if (_sph2Mesh != null)
                {
                    e.Display.DrawMeshShaded(_sph2Mesh, _sphColor);
                }
            }
        }

        public void Dispose()
        {
            RhinoDoc.LayerTableEvent -= RhinoDoc_LayerTableEvent;
            _sph1Mesh?.Dispose();
            _sph2Mesh?.Dispose();
        }
    }
}
