#if DEBUG

using IDS.Core.Operations;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDS.Commands.Hidden
{
    [System.Runtime.InteropServices.Guid("54D82E2B-0601-4AC6-8019-52B4F6650707")]
    public class CircleDesignerTest : Command
    {
        public CircleDesignerTest()
        {
            Instance = this;
        }

        public static CircleDesignerTest Instance { get; private set; }

        public override string EnglishName => "CircleDesignerTest";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            const int innerCupRadius = 27;
            var circleCenter = new Point3d(0, innerCupRadius, 0);
            var circleDesigner = new CircleDesigner(circleCenter);
            var curve = circleDesigner.CreateCurveOnCircle(innerCupRadius + 4, 85);

            RhinoDoc.ActiveDoc.Objects.AddCurve(curve);
            RhinoDoc.ActiveDoc.Views.Redraw();

            return Result.Success;
        }
    }
}

#endif