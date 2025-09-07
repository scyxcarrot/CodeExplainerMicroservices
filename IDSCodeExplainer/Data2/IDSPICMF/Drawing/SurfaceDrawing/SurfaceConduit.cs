using IDS.CMF.DataModel;
using Rhino.Display;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.PICMF.Drawing
{
    public class SurfaceConduit : DisplayConduit
    {
        private readonly List<PatchData> _patchSurfaces;
        private readonly List<PatchData> _skeletonSurfaces;

        public bool IsHighlighted { get; set; }

        public SurfaceConduit(
            List<PatchData> patchSurfaces, 
            List<PatchData> skeletonSurfaces)
        {
            _patchSurfaces = patchSurfaces;
            _skeletonSurfaces = skeletonSurfaces;
            IsHighlighted = false;
        }
        
        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);
            _patchSurfaces.ForEach(m => DrawSurface(e, m, Color.Red));
            _skeletonSurfaces.ForEach(s => DrawSurface(e, s, Color.Yellow));
        }

        private void DrawSurface(DrawEventArgs e, PatchData patchData, Color color)
        {
            if (IsHighlighted)
            {
                e.Display.DrawMeshShaded(patchData.Patch, CreateMaterial(color));
                patchData.Edges.ForEach(l =>
                {
                    e.Display.DrawPolyline(l, color, 3);
                });
            }
            else
            {
                e.Display.DrawMeshShaded(patchData.Patch, CreateMaterial(color));
                patchData.Edges.ForEach(l =>
                {
                    e.Display.DrawPolyline(l, color, 1);
                });
            }
        }

        private DisplayMaterial CreateMaterial(Color color)
        {
            var mat = new DisplayMaterial
            {
                Transparency = 0.5,
                Diffuse = color,
                Specular = color,
                Emission = color
            };
            return mat;
        }
    }
}
