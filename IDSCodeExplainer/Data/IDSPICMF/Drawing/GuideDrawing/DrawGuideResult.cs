using IDS.CMF.DataModel;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.PICMF.Drawing
{
    public class DrawGuideResult
    {
        public Mesh RoIMesh { get; set; }
        public List<PatchData> GuideBaseSurfaces { get; private set; } = new List<PatchData>();
        public List<PatchData> GuideBaseNegativeSurfaces { get; private set; } = new List<PatchData>();
    }
}
