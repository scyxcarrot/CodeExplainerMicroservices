using IDS.CMF.DataModel;
using IDS.CMF.Visualization;

namespace IDS.PICMF.Drawing
{
    public class EditGuidePositivePatchMode : EditGuidePatch
    {
        private readonly DrawGuideDataContext _dataContext;

        public EditGuidePositivePatchMode(ref DrawGuideDataContext dataContext, PatchSurface patchSurface) :
            base(dataContext.PatchSurfaces, ref dataContext.PatchTubeDiameter, patchSurface)
        {
            _dataContext = dataContext;
            feedbackTubeColor = Colors.GuidePositivePatchWireframe;
            meshWireColor = Colors.GuidePositivePatchWireframe;
        }
    }
}
