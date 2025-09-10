using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Operations
{
    public class ImplantSupportRoIRemainedMetalPartsIntegrator
    {
        public List<Mesh> MetalParts { get; set; }

        public Mesh Result { get; private set; }

        public double ResultingOffset { get; set; }

        public ImplantSupportRoIRemainedMetalPartsIntegrator()
        {
            MetalParts = new List<Mesh>();
        }

        public bool Execute()
        {
            if (!MetalParts.Any())
            {
                return false;
            }

            Result = null;

            Mesh wrappedMetals;
            if (!Wrap.PerformWrap(MetalParts.ToArray(), 0.2, 0, ResultingOffset, false, false, false, false, out wrappedMetals))
            {
                return false;
            }

            Result = wrappedMetals;
            return true;
        }
    }
}
