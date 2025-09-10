using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace IDS.Core.Drawing
{
    public class MeshConduit : DisplayConduit, IDisposable
    {
        private Mesh displayMesh;
        private DisplayMaterial displayMaterial;
        private BoundingBox boundingBox;
        private readonly bool _drawForeground;
        
        public MeshConduit() : this(false)
        {

        }

        public MeshConduit(bool drawForeground)
        {
            boundingBox = BoundingBox.Unset;
            _drawForeground = drawForeground;
        }

        public void SetMesh(Mesh mesh, Color color, double transparency)
        {
            displayMesh = mesh;
            boundingBox = displayMesh.GetBoundingBox(true);
            if (displayMaterial == null)
            {
                displayMaterial = new DisplayMaterial(color, transparency);
            }

            displayMaterial.Diffuse = color;
            displayMaterial.Specular = color;
            displayMaterial.Ambient = color;
            displayMaterial.Emission = color;
            displayMaterial.Transparency = transparency;
        }

        public void ResetMesh()
        {
            displayMesh = null;
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);
            if (!_drawForeground)
            {
                Draw(e);
            }
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            base.DrawForeground(e);
            if (_drawForeground)
            {
                Draw(e);
            }
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            e.IncludeBoundingBox(boundingBox);
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
                displayMaterial.Dispose();
            }
        }

        private void Draw(DrawEventArgs e)
        {
            if (displayMesh != null && displayMaterial != null)
            {
                e.Display.DrawMeshShaded(displayMesh, displayMaterial);
            }
        }
    }
}