using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class ImplantSupportRoITeethPartsIntegrator
    {
        public List<Mesh> TeethParts { get; set; }

        public Mesh Result { get; private set; }

        public double ResultingOffset { get; set; }

        public ImplantSupportRoITeethPartsIntegrator()
        {
            TeethParts = new List<Mesh>();
        }

        public bool Execute()
        {
            if (!TeethParts.Any())
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

            Result = wrappedTeeth;
            return true;
        }
    }
}
