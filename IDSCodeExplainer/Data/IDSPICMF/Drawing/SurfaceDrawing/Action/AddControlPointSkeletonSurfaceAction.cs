using Rhino.Geometry;

namespace IDS.PICMF.Drawing
{
    public class AddControlPointSkeletonSurfaceAction : IUndoableSurfaceAction
    {
        private int _selectedIndex;
        public Point3d PointToAdd { get; set; }

        public bool Do(DrawSurfaceUndoData data)
        {
            _selectedIndex = data.SelectedIndex;
            data.AllPoints.Add(PointToAdd);
            data.CurrentPointList.Add(PointToAdd);
            data.SelectedIndex = data.AllPoints.IndexOf(PointToAdd);
            return true;
        }

        public bool Undo(DrawSurfaceUndoData data)
        {
            data.AllPoints.Remove(PointToAdd);
            data.CurrentPointList.Remove(PointToAdd);
            data.SelectedIndex = _selectedIndex;
            return true;
        }
    }
}