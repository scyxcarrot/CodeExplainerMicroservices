using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;
using IDS.Core.Utilities;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.PICMF.Operations
{
    public class ImplantSupportRoIRemovedMetalPartsIntegrator
    {
        public List<Mesh> BoneParts { get; set; } //can set from the caller

        public List<Mesh> MetalParts { get; set; }

        public Mesh Result { get; private set; }

        public double ResultingOffset { get; set; }

        public ImplantSupportRoIRemovedMetalPartsIntegrator()
        {
            BoneParts = new List<Mesh>();
            MetalParts = new List<Mesh>();
        }

        public bool Execute()
        {
            if (!BoneParts.Any() || !MetalParts.Any())
            {
                return false;
            }

            Result = null;

            Mesh wrappedRoI;
            if (!Wrap.PerformWrap(BoneParts.ToArray(), 2.0, 20.0, ResultingOffset, false, true, false, false, out wrappedRoI))
            {
                return false;
            }

            var metalParts = MeshUtilities.AppendMeshes(MetalParts);
            var intersection = Booleans.PerformBooleanIntersection(wrappedRoI, metalParts);

#if (INTERNAL)
            InternalUtilities.ReplaceObject(intersection, "Intermediate - MetalIntegration"); //temporary
#endif

            Result = intersection;

            return true;
        }
    }
}
