using Rhino.Geometry;

namespace IDS.PICMF.Drawing
{
    public class AddControlPointSurfaceAction : IUndoableSurfaceAction
    {
        public Point3d PointToAdd { get; set; }
        public bool Do(DrawSurfaceUndoData data)
        {
            data.CurrentPointList.Add(PointToAdd);
            return true;
        }
        public bool Undo(DrawSurfaceUndoData data)
        {
            data.CurrentPointList.Remove(PointToAdd);
            return true;
        }
    }
}
