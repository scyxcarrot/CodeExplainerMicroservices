using IDS.CMF.Visualization;
using IDS.Core.Utilities;
using Rhino.Display;
using Rhino.Input.Custom;
using System.Drawing;

namespace IDS.PICMF.Drawing
{
    public class DrawGuideNegativePatchMode : DrawGuidePatch
    {
        public DrawGuideNegativePatchMode(ref DrawGuideDataContext dataContext) : base(dataContext.NegativePatchSurfaces, ref dataContext.NegativePatchTubeDiameter)
        {
            _dataContext = dataContext;

            _isNegative = true;
            closingPointColor = Color.RosyBrown;
            pointsColor = Color.YellowGreen;
            curveColor = Color.Maroon;
            feedbackTubeColor = Colors.GuideNegativePatchWireframe;
            meshWireColor = Colors.GuideNegativePatchWireframe;
        }

        public override void OnDynamicDraw(GetPointDrawEventArgs e, GetCurvePoints drawCurvePointsDerivation)
        {
            base.OnDynamicDraw(e, drawCurvePointsDerivation);
            e.Display.DrawPoint(e.CurrentPoint, PointStyle.Circle, 5, meshWireColor);
        }
    }
}
