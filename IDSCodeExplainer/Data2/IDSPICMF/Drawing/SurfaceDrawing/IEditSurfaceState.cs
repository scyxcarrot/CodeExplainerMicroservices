using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace IDS.PICMF.Drawing
{
    public interface IEditSurfaceState
    {
        void OnKeyboard(int key, EditSurface editSurface);

        bool OnGetPoint(Point3d point, EditSurface editSurface);

        void OnDynamicDraw(GetPointDrawEventArgs e, EditSurface editSurface);
        void OnPostDrawObjects(DrawEventArgs e, EditSurface editSurface);

        void OnMouseMove(GetPointMouseEventArgs e, EditSurface editSurface);
        void OnMouseLeave(RhinoView view, EditSurface editSurface);
        void OnMouseEnter(RhinoView view, EditSurface editSurface);

        bool OnFinalize(EditSurface editSurface, out bool isContinueLooping);

        void OnExecute(EditSurface editSurface);
    }
}
