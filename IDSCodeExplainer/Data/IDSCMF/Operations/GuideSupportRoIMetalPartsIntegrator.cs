using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;
using IDS.Core.Utilities;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Operations
{
    //Just proposal, you get the idea
    public class GuideSupportRoIMetalPartsIntegrator
    {
        public List<Mesh> AdditionalGuideRoIBaseParts { get; set; } //can set from the caller

        public List<Mesh> MetalParts { get; set; }

        public Mesh Result { get; private set; } //can change

        public double ResultingOffset { get; set; }

        public GuideSupportRoIMetalPartsIntegrator()
        {
            AdditionalGuideRoIBaseParts = new List<Mesh>();
            MetalParts = new List<Mesh>();
        }

        public bool Execute()
        {
            if (!AdditionalGuideRoIBaseParts.Any() || !MetalParts.Any())
            {
                return false;
            }

            Result = null;

            // metal integration
            // 1. Wrap RoI with selected resulting offset ==> #1
            // 2. Boolean intersect selected metal parts with #1 ==> #2

            Mesh wrappedRoI;
            if (!Wrap.PerformWrap(AdditionalGuideRoIBaseParts.ToArray(), 2.0, 20.0, ResultingOffset, false, true, false, false, out wrappedRoI))
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
