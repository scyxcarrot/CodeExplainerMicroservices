using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Utilities;
using IDS.Interface.Implant;
using IDS.PICMF.Drawing;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.DrawingAction
{
    public class DeleteControlPointAction : IUndoableAction
    {
        private List<IConnection> removedConnections;
        private IDot removedDot;
        private int indexOfRemovedDot;
        private List<IConnection> addedConnections;

        public Point3d PointToDelete { get; set; }

        public bool Do(DrawImplantBaseState state)
        {
            if (!state.DataModelBase.ConnectionList.Any())
            {
                return false;
            }

            var ctrlPoint = ImplantCreationUtilities.FindClosestControlPoint(state.DataModelBase.DotList, PointToDelete);
            if (ctrlPoint == null || DataModelUtilities.DistanceBetween(ctrlPoint.Location, PointToDelete) > 1.0)
            {
                return false;
            }

            var connecteds = ConnectionUtilities.FindConnectionsTheDotsBelongsTo(state.DataModelBase.ConnectionList, ctrlPoint);
            var dots = ConnectionUtilities.FindNeighbouringDots(state.DataModelBase.ConnectionList, ctrlPoint);

            if (dots.Count < 2)
            {
                return false;
            }

            var prevPlateWidth = state.PlateWidth;
            var prevLinkWidth = state.LinkWidth;
            var prevPlate = state.CreatePlate;

            if (connecteds[0] is ConnectionPlate)
            {
                state.CreatePlate = true;
                state.PlateWidth = connecteds[0].Width;
            }
            else
            {
                state.CreatePlate = false;
                state.LinkWidth = connecteds[0].Width;
            }

            state.ConnectionThickness = connecteds[0].Thickness;

            removedDot = ctrlPoint;
            removedConnections = connecteds;
            addedConnections = new List<IConnection>();

            removedConnections.ForEach(x => state.DataModelBase.ConnectionList.Remove(x));
            indexOfRemovedDot = state.DataModelBase.DotList.IndexOf(removedDot);
            state.DataModelBase.DotList.Remove(removedDot);

            for (var i = 0; i < dots.Count - 1; i++)
            {
                var width = state.CreatePlate ? state.PlateWidth : state.LinkWidth;
                var conn = ImplantCreationUtilities.CreateConnection(dots[i], dots[i + 1], state.ConnectionThickness, width, state.CreatePlate);
                state.DataModelBase.ConnectionList.Add(conn);
                addedConnections.Add(conn);
            }

            state.PlateWidth = prevPlateWidth;
            state.LinkWidth = prevLinkWidth;
            state.CreatePlate = prevPlate;

            return true;
        }

        public bool Undo(DrawImplantBaseState state)
        {
            if (removedDot != null && removedConnections != null && addedConnections != null)
            {
                foreach (var conn in addedConnections)
                {
                    state.DataModelBase.ConnectionList.Remove(conn);
                }
                
                state.DataModelBase.DotList.Insert(indexOfRemovedDot, removedDot);
                removedConnections.ForEach(x => state.DataModelBase.ConnectionList.Add(x));
            }

            return true;
        }
    }
}
