using IDS.CMF.DataModel;
using System.Collections.Generic;

namespace IDS.PICMF.Drawing
{
    public class DeleteGuideResult
    {
        public List<PatchData> GuideBaseSurfaces { get; private set; } = new List<PatchData>();
        public List<PatchData> GuideBaseNegativeSurfaces { get; private set; } = new List<PatchData>();
        public List<PatchData> GuideLinkSurfaces { get; private set; } = new List<PatchData>();
        public List<PatchData> GuideSolidSurfaces { get; private set; } = new List<PatchData>();

        public DeleteGuideResult(List<PatchData> guideBaseSurfaces, List<PatchData> guideBaseNegativeSurfaces,
            List<PatchData> guideLinkSurfaces, List<PatchData> guideSolidSurfaces)
        {
            GuideBaseSurfaces = guideBaseSurfaces;
            GuideBaseNegativeSurfaces = guideBaseNegativeSurfaces;
            GuideLinkSurfaces = guideLinkSurfaces;
            GuideSolidSurfaces = guideSolidSurfaces;
        }
    }
}
