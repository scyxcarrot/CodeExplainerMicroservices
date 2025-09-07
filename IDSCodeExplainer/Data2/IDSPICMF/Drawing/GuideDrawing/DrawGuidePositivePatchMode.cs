using IDS.CMF.Visualization;
using IDS.Core.Utilities;
using Rhino.Display;
using Rhino.Input.Custom;

namespace IDS.PICMF.Drawing
{
    public class DrawGuidePositivePatchMode : DrawGuidePatch
    {

        public DrawGuidePositivePatchMode(ref DrawGuideDataContext dataContext, bool drawSolidSurface = false) : base(dataContext.PatchSurfaces, ref dataContext.PatchTubeDiameter)
        {
            _dataContext = dataContext;
            _isNegative = false;

            if (drawSolidSurface)
            {
                feedbackTubeColor = Colors.GuideSolidPatch;
                meshWireColor = Colors.GuideSolidPatch;
            }
            else
            {
                feedbackTubeColor = Colors.GuidePositivePatchWireframe;
                meshWireColor = Colors.GuidePositivePatchWireframe;
            }
        }

        public override void OnDynamicDraw(GetPointDrawEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {
            base.OnDynamicDraw(e, drawCurvePointsDerivation);
            e.Display.DrawPoint(e.CurrentPoint, PointStyle.Circle, 5, meshWireColor);
        }
    }

    public class DrawGuidePositivePatchGuideRoIMode : DrawGuidePositivePatchMode
    {
        public DrawGuidePositivePatchGuideRoIMode(ref DrawGuideDataContext dataContext) : base(ref dataContext)
        {
            _dataContext = dataContext;
            _isNegative = false;
            feedbackTubeColor = Colors.GuidePositivePatchWireframe;
            meshWireColor = Colors.GuidePositivePatchWireframe;
        }


    }
}
