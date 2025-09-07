using IDS.CMF.Utilities;
using IDS.Interface.Implant;
using IDS.PICMF.Drawing;
using Rhino.Geometry;
using System.Linq;

namespace IDS.PICMF.DrawingAction
{
    public abstract class InsertDotAction : IUndoableAction
    {
        private IConnection removedConnection;
        private ImplantCreationUtilities.SplitConnectionDataModel addedSplitConnection;
        private IDot lastDot;

        public Point3d PointToInsert { get; set; }
        public Mesh ConstraintMesh { get; set; }

        public bool Do(DrawImplantBaseState state)
        {
            if (!state.DataModelBase.ConnectionList.Any())
            {
                return false;
            }

            var meshPoint = ConstraintMesh.ClosestMeshPoint(PointToInsert, 0.0001);
            var pointOnMesh = meshPoint.Point;
            var normalAtPoint = ConstraintMesh.FaceNormals[meshPoint.FaceIndex];
            var connection = ImplantCreationUtilities.FindClosestConnection(state.DataModelBase.ConnectionList, pointOnMesh);
            var tolerance = connection.Width > connection.Thickness ? connection.Width : connection.Thickness;
            var splitted = SplitConnection(connection, pointOnMesh, normalAtPoint, tolerance * 2);

            if (splitted == null)
            {
                return false;
            }

            removedConnection = connection;
            addedSplitConnection = splitted;
            state.DataModelBase.ConnectionList.Remove(removedConnection);
            state.DataModelBase.ConnectionList.Add(addedSplitConnection.FirstHalf);
            state.DataModelBase.ConnectionList.Add(addedSplitConnection.SecondHalf);

            lastDot = state.DataModelBase.DotList.Last();
            state.DataModelBase.DotList.Remove(lastDot);
            state.DataModelBase.DotList.Add(addedSplitConnection.NewDot);
            state.DataModelBase.DotList.Add(lastDot);

            return true;
        }

        public bool Undo(DrawImplantBaseState state)
        {
            if (addedSplitConnection != null)
            {
                state.DataModelBase.DotList.Remove(lastDot);
                state.DataModelBase.DotList.Remove(addedSplitConnection.NewDot);
                state.DataModelBase.DotList.Add(lastDot);

                state.DataModelBase.ConnectionList.Remove(addedSplitConnection.SecondHalf);
                state.DataModelBase.ConnectionList.Remove(addedSplitConnection.FirstHalf);
                state.DataModelBase.ConnectionList.Add(removedConnection);
            }

            return true;
        }

        protected abstract ImplantCreationUtilities.SplitConnectionDataModel SplitConnection(IConnection connection, Point3d point, Vector3d normal, double tolerance);
    }
}
