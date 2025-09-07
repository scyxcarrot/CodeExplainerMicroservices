using IDS.PICMF.Forms;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace IDS.PICMF.Visualization
{
    public class SupportRoIConduit : DisplayConduit, IDisposable
    {
        protected SupportRoICreationDataModel dataModel;
        private readonly DisplayMaterial _integratedTeethMaterial;

        public SupportRoIConduit(SupportRoICreationDataModel dataModel)
        {
            this.dataModel = dataModel;
            _integratedTeethMaterial = CreateTeethMaterial();
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);

            IncludeNotAccurateBoundingBox(e, dataModel.Teeth.IntegratedTeeth);
        }

        public static DisplayMaterial CreateTeethMaterial()
        {
            return CreateMaterial(0.05, Color.LightPink);
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);

            DrawMesh(e, dataModel.Teeth.IntegratedTeeth, _integratedTeethMaterial);
        }

        protected void DrawMesh(DrawEventArgs e, Mesh mesh, DisplayMaterial material)
        {
            if (mesh != null)
            {
                e.Display.DrawMeshShaded(mesh, material);
            }
        }

        protected void IncludeNotAccurateBoundingBox(CalculateBoundingBoxEventArgs e, Mesh mesh)
        {
            if (mesh != null)
            {
                e.IncludeBoundingBox(mesh.GetBoundingBox(false));
            }
        }

        public void CleanUp()
        {
            dataModel = null;
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
                _integratedTeethMaterial.Dispose();
            }
        }

        public static DisplayMaterial CreateMaterial(double transparency, Color color)
        {
            var displayMaterial = new DisplayMaterial
            {
                Transparency = transparency,
                Diffuse = color,
                Specular = color
            };

            return displayMaterial;
        }
    }
}
