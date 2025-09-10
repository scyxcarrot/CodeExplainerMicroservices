using IDS.Core.Utilities;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace IDS.PICMF.Drawing
{
    public interface IDrawSurfaceState
    {
        void OnKeyboard(int key, DrawSurface drawSurface);
        bool OnGetPoint(Point3d point, Mesh constraintMesh, GetCurvePoints drawCurvePointsDerivation);
        void OnDynamicDraw(GetPointDrawEventArgs e, GetCurvePoints drawCurvePointsDerivation);
        void OnPostDrawObjects(DrawEventArgs e, GetCurvePoints drawCurvePointsDerivation);
        void OnMouseMove(GetPointMouseEventArgs e, GetCurvePoints drawCurvePointsDerivation);
        void OnMouseLeave(RhinoView view);
        void OnMouseEnter(RhinoView view);
        bool OnFinalize(Mesh constraintMesh, out bool isContinueLooping);
        void OnUndo(Mesh constraintMesh);
    }
}
