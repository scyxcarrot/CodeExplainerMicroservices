using IDS.CMF.DataModel;
using IDS.CMF.Visualization;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace IDS.PICMF.Visualization
{
    public class GuideSupportRoIWrapConduit : DisplayConduit, IDisposable
    {
        private SupportCreationDataModel _dataModel;
        private readonly DisplayMaterial _roIMaterial;
        private readonly DisplayMaterial _previewWrapMaterial;

        public GuideSupportRoIWrapConduit(SupportCreationDataModel dataModel)
        {
            _dataModel = dataModel;
            _roIMaterial = CreateMaterial(0.05, Colors.GeneralGrey, false);
            _previewWrapMaterial = CreateMaterial(0.4, Color.FromArgb(0, 128, 255), true);
        }

        protected override void CalculateBoundingBox(Rhino.Display.CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);

            IncludeNotAccurateBoundingBox(e, _dataModel.InputRoI);
            IncludeNotAccurateBoundingBox(e, _dataModel.WrapRoI1);
        }


        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);

            DrawMesh(e, _dataModel.InputRoI, _roIMaterial); 
            
            DrawMesh(e, _dataModel.WrapRoI1, _previewWrapMaterial);
        }

        private void DrawMesh(DrawEventArgs e, Mesh mesh, DisplayMaterial material)
        {
            if (mesh != null)
            {
                e.Display.DrawMeshShaded(mesh, material);
            }
        }

        private void IncludeNotAccurateBoundingBox(CalculateBoundingBoxEventArgs e, Mesh mesh)
        {
            if (mesh != null)
            {
                e.IncludeBoundingBox(mesh.GetBoundingBox(false));
            }
        }

        public void CleanUp()
        {
            _dataModel = null;
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
                _roIMaterial.Dispose();
                _previewWrapMaterial.Dispose();
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
