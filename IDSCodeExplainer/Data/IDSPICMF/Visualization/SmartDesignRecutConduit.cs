using IDS.PICMF.Forms;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Visualization
{
    public class SmartDesignRecutConduit : DisplayConduit, IDisposable
    {
        private IRecutViewModel _viewModel;
        private Dictionary<string, DisplayMaterial> _displayMaterials;

        public SmartDesignRecutConduit()
        {
            _displayMaterials = new Dictionary<string, DisplayMaterial>();
        }

        public void SetViewModel(IRecutViewModel viewModel)
        {
            ClearDisplayMaterial();
            _viewModel = viewModel;

            foreach (var part in _viewModel.PartSelections)
            {
                _displayMaterials.Add(part.Key, CreateMaterial(part.Value.Color));
            }
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);

            if (_viewModel == null || !_viewModel.PartSelections.Any())
            {
                return;
            }

            foreach (var part in _viewModel.PartSelections)
            {
                foreach (var mesh in part.Value.SelectedMeshes)
                {
                    IncludeNotAccurateBoundingBox(e, mesh);
                }
            }
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);

            if (_viewModel == null || !_viewModel.PartSelections.Any())
            {
                return;
            }

            foreach (var part in _viewModel.PartSelections)
            {
                foreach (var mesh in part.Value.SelectedMeshes)
                {
                    DrawMesh(e, mesh, _displayMaterials[part.Key]);
                }
            }  
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
            _viewModel = null;
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
                ClearDisplayMaterial();
            }
        }

        private void ClearDisplayMaterial()
        {
            foreach (var material in _displayMaterials.Values)
            {
                material.Dispose();
            }
            _displayMaterials.Clear();
        }

        private DisplayMaterial CreateMaterial(Color color)
        {
            var displayMaterial = new DisplayMaterial
            {
                Diffuse = color,
                Specular = color
            };

            return displayMaterial;
        }
    }
}
