using IDS.CMF.Utilities;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class GuideCutSlotCreator
    {
        public Mesh ResGuideBaseWithCutSlot { get; private set; }

        public bool CreateCutSlots(Mesh lightWeightSurface, Mesh surfaceWrap, double lightWeightRadius, List<Mesh> guideLinks, Mesh osteotomyMesh)
        {
            if (osteotomyMesh != null && guideLinks.Any())
            {
                var offsettedLinkMesh = CreateOffset(lightWeightRadius, guideLinks, surfaceWrap);

                var trimmedOsteotomyMesh = Booleans.PerformBooleanSubtraction(osteotomyMesh, offsettedLinkMesh);
                trimmedOsteotomyMesh.Faces.CullDegenerateFaces();

                ResGuideBaseWithCutSlot = Booleans.PerformBooleanSubtraction(lightWeightSurface, trimmedOsteotomyMesh);
            }
            else if (osteotomyMesh != null && !guideLinks.Any())
            {
                ResGuideBaseWithCutSlot = Booleans.PerformBooleanSubtraction(lightWeightSurface, osteotomyMesh);
            }
            else
            {
                ResGuideBaseWithCutSlot = lightWeightSurface;
            }
            return true;
        }

        private Mesh CreateOffset(double lightWeightRadius, List<Mesh> guideLinks, Mesh surfaceWrap)
        {
            var offset = (lightWeightRadius * 2) + 0.5;

            var surfaces = GetPatches(guideLinks, surfaceWrap);

            var offsettedLinkMesh = GuideSurfaceUtilities.CreateOffset(surfaces, offset);

            var remeshed = Remesh.PerformRemesh(offsettedLinkMesh, 0.0, 0.2, 0.2, 0.01, 0.3, false, 3);
            var smoothen = ExternalToolInterop.PerformSmoothing(remeshed, true, true, false, 30.0, 0.7, 3);

            if (!Wrap.PerformWrap(new Mesh[] { smoothen }, 0.1, 0.0, 0.0, false, true, false, false, out offsettedLinkMesh))
            {
                throw new IDSException("Failed to create wrap for guide link during cut slot creation");
            }

            remeshed.Dispose();
            smoothen.Dispose();

            return offsettedLinkMesh;
        }

        private List<Mesh> GetPatches(List<Mesh> guideLinks, Mesh surfaceWrap)
        {
            var patches = new List<Mesh>();

            foreach (var surface in guideLinks)
            {
                surface.Faces.CullDegenerateFaces();

                var borders = surface.GetNakedEdges().Select(x => x.ToNurbsCurve()).Where(x => x.IsClosed);

                var generated = true;
                var regeneratedSurfaces = new List<Mesh>();

                foreach (var border in borders)
                {
                    //get LoD High surface
                    var patch = SurfaceUtilities.GetPatch(surfaceWrap, border);
                    if (patch != null)
                    {
                        regeneratedSurfaces.Add(patch);
                    }
                    else
                    {
                        generated = false;
                        break;
                    }
                }

                if (generated)
                {
                    patches.AddRange(regeneratedSurfaces);
                }
                else
                {
                    patches.Add(surface);
                }
            }

            return patches;
        }
    }
}
