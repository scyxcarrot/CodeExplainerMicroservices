using IDS.CMF.DataModel;
using IDS.CMF.Visualization;

namespace IDS.PICMF.Drawing
{
    public class EditGuideNegativePatchMode : EditGuidePatch
    {
        private readonly DrawGuideDataContext _dataContext;

        public EditGuideNegativePatchMode(ref DrawGuideDataContext dataContext, PatchSurface patchSurface) :
            base(dataContext.NegativePatchSurfaces, ref dataContext.NegativePatchTubeDiameter, patchSurface)
        {
            _dataContext = dataContext;

            _isNegative = true;
            feedbackTubeColor = Colors.GuideNegativePatchWireframe;
            meshWireColor = Colors.GuideNegativePatchWireframe;
        }
    }
}
