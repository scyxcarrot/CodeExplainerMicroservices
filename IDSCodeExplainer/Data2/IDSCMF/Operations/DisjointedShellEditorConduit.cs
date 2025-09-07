using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace IDS.CMF.Operations
{
    public partial class DisjointedShellEditor
    {
        protected class DisjointedShellEditorConduit : DisplayConduit, IDisposable
        {
            private readonly DisplayMaterial _defaultRemainMaterial;
            private readonly DisplayMaterial _defaultRejectMaterial;
            private readonly DisjointedShellEditorDataModel _disjointedShellEditorDataModel;

            public bool Show
            {
                get => Enabled;
                set
                {
                    Enabled = value;
                    RhinoDoc.ActiveDoc.Views.Redraw();
                }
            }

            public DisjointedShellEditorConduit(DisjointedShellEditorDataModel disjointedShellEditorDataModel)
            {
                _defaultRemainMaterial = CreateMaterial(0, Color.DarkBlue, false);
                _defaultRejectMaterial = CreateMaterial(0.05, Color.DarkOrange, false);
                _disjointedShellEditorDataModel = disjointedShellEditorDataModel;
            }

            protected override void CalculateBoundingBox(Rhino.Display.CalculateBoundingBoxEventArgs e)
            {
                base.CalculateBoundingBox(e);

                var disjointedShellInfos = _disjointedShellEditorDataModel.GetDisjointedShellInfos();
                foreach (var disjointedShellInfo in disjointedShellInfos)
                {
                    IncludeBoundingBox(e, disjointedShellInfo.DisjointedMeshBoundingBox);
                }
            }

            protected override void PostDrawObjects(DrawEventArgs e)
            {
                base.PostDrawObjects(e);

                var disjointedShellInfos = _disjointedShellEditorDataModel.GetDisjointedShellInfos();
                foreach (var disjointedShellInfo in disjointedShellInfos)
                {
                    DrawMesh(e, disjointedShellInfo.DisjointedMesh, disjointedShellInfo.Keep, disjointedShellInfo.OwnRemainMaterial, disjointedShellInfo.OwnRejectMaterial);
                }
            }

            private void DrawMesh(DrawEventArgs e, Mesh mesh, bool keep, DisplayMaterial ownRemainMaterial, DisplayMaterial ownRejectMaterial)
            {
                if (mesh != null)
                {
                    e.Display.DrawMeshShaded(mesh, keep ? ownRemainMaterial ?? _defaultRemainMaterial : ownRejectMaterial ?? _defaultRejectMaterial);
                }
            }

            private void IncludeBoundingBox(CalculateBoundingBoxEventArgs e, BoundingBox bbox)
            {
                if (bbox.IsValid)
                {
                    e.IncludeBoundingBox(bbox);
                }
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
                    _defaultRemainMaterial.Dispose();
                    _defaultRejectMaterial.Dispose();
                }
            }

            private DisplayMaterial CreateMaterial(double transparency, Color color, bool setEmission)
            {
                var displayMaterial = new DisplayMaterial
                {
                    Transparency = transparency,
                    Diffuse = color,
                    Specular = color
                };

                if (setEmission)
                {
                    displayMaterial.Emission = color;
                }

                return displayMaterial;
            }
        }
    }
}
