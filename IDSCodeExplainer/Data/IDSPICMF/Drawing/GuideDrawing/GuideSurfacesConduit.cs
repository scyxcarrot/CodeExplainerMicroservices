using IDS.CMF.DataModel;
using IDS.CMF.Visualization;
using Rhino.Display;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.PICMF.Drawing
{
    public class GuideSurfacesConduit : DisplayConduit
    {
        private readonly List<PatchData> _patchSurfaces;
        private readonly List<PatchData> _negativePatchSurfaces;
        private readonly List<PatchData> _skeletonSurfaces;
        private readonly List<PatchData> _solidSurfaces;

        private readonly List<PatchData> _surfaceRenderingExclusion = new List<PatchData>();

        public List<PatchData> PatchSurfaceExclusion { get; private set; } = new List<PatchData>();

        public bool IsHighlighted { get; set; }

        public GuideSurfacesConduit(List<PatchData> patchSurfaces, List<PatchData> negativePatchSurfaces, List<PatchData> skeletonSurfaces, List<PatchData> solidSurfaces)
        {
            _patchSurfaces = patchSurfaces;
            _negativePatchSurfaces = negativePatchSurfaces;
            _skeletonSurfaces = skeletonSurfaces;
            _solidSurfaces = solidSurfaces;

            IsHighlighted = false;
        }
        
        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);
            
            _patchSurfaces.ForEach(m => DrawSurface(e, m, Colors.GuidePositivePatchWireframe));

            _negativePatchSurfaces.ForEach(m => DrawSurface(e, m, Colors.GuideNegativePatchWireframe));

            _skeletonSurfaces.ForEach(s => DrawSurface(e, s, Colors.GuidePositiveSkeletonSurfaces));

            _solidSurfaces.ForEach(s => DrawSurface(e, s, Colors.GuideSolidPatch));
        }

        private void DrawSurface(DrawEventArgs e, PatchData patchData, Color color)
        {
            if (_surfaceRenderingExclusion.Contains(patchData))
            {
                return;
            }

            if (PatchSurfaceExclusion.Exists(x => x == patchData))
            {
                e.Display.DrawMeshShaded(patchData.Patch, CreateMaterial(Color.CornflowerBlue));
                patchData.Edges.ForEach(l =>
                {
                    e.Display.DrawPolyline(l, Color.DarkBlue, 1);
                });
                return;
            }

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

        public void AddSurfaceToNotRender(PatchData surface)
        {
            if (!_surfaceRenderingExclusion.Contains(surface))
            {
                _surfaceRenderingExclusion.Add(surface);
            }
        }

        public void ResetSurfaceToNotRender()
        {
            _surfaceRenderingExclusion.Clear();
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
