using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Operations
{
    public class GuideSupportRoITeethPartsIntegrator
    {
        public List<Mesh> AdditionalGuideRoIBaseParts { get; set; } //can set from the caller

        public List<Mesh> TeethParts { get; set; }

        public Mesh Result { get; private set; } //can change

        public double ResultingOffset { get; set; }

        public GuideSupportRoITeethPartsIntegrator()
        {
            AdditionalGuideRoIBaseParts = new List<Mesh>();
            TeethParts = new List<Mesh>();
        }

        public bool Execute()
        {
            if (!AdditionalGuideRoIBaseParts.Any() || !TeethParts.Any())
            {
                return false;
            }

            Result = null;

            // teeth integration
            // 1. Wrap selected teeth parts with selected resulting offset ==> #1

            Mesh wrappedTeeth;
            if (!Wrap.PerformWrap(TeethParts.ToArray(), 0.3, 3.0, ResultingOffset, false, true, false, false, out wrappedTeeth))
            {
                return false;
            }

#if (INTERNAL)
            InternalUtilities.ReplaceObject(wrappedTeeth, "Intermediate - TeethIntegration"); //temporary
#endif

            Result = wrappedTeeth;
            return true;
        }
    }
}
