using Rhino.Geometry;

namespace IDS.PICMF.DrawingAction
{
    public class AddControlPointGuideAction : IUndoableGuideAction
    {
        public Point3d PointToAdd { get; set; }

        public bool Do(DrawGuideUndoData data)
        {
            data.CurrentPointList.Add(PointToAdd);
            return true;
        }

        public bool Undo(DrawGuideUndoData data)
        {
            data.CurrentPointList.Remove(PointToAdd);
            return true;
        }
    }
}
