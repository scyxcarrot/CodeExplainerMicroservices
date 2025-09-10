using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace IDS.PICMF.Drawing
{
    public interface IEditGuideState
    {
        void OnKeyboard(int key, EditGuide editGuide);

        bool OnGetPoint(Point3d point, EditGuide editGuide);

        void OnDynamicDraw(GetPointDrawEventArgs e, EditGuide editGuide);
        void OnPostDrawObjects(DrawEventArgs e, EditGuide editGuide);

        void OnMouseMove(GetPointMouseEventArgs e, EditGuide editGuide);
        void OnMouseLeave(RhinoView view, EditGuide editGuide);
        void OnMouseEnter(RhinoView view, EditGuide editGuide);

        bool OnFinalize(EditGuide editGuide, out bool isContinueLooping);

        void OnExecute(EditGuide editGuide);
    }
}
