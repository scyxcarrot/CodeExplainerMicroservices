using Rhino.Geometry;

namespace IDS.PICMF.DrawingAction
{
    public class AddControlPointSkeletonGuideAction : IUndoableGuideAction
    {
        private int selectedIndex;
        public Point3d PointToAdd { get; set; }

        public bool Do(DrawGuideUndoData data)
        {       
            selectedIndex = data.SelectedIndex;
            data.AllPoints.Add(PointToAdd);
            data.CurrentPointList.Add(PointToAdd);
            data.SelectedIndex = data.AllPoints.IndexOf(PointToAdd);
            return true;
        }

        public bool Undo(DrawGuideUndoData data)
        {
            data.AllPoints.Remove(PointToAdd);
            data.CurrentPointList.Remove(PointToAdd);
            data.SelectedIndex = selectedIndex;
            return true;
        }
    }
}
