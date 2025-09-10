using IDS.CMF.Utilities;
using IDS.Interface.Implant;
using Rhino.Geometry;

namespace IDS.PICMF.DrawingAction
{
    public class InsertScrewAction : InsertDotAction
    {
        public Point3d ScrewToInsert
        {
            get { return PointToInsert; }
            set { PointToInsert = value; }
        }

        public double PastilleDiameter { get; set; }

        protected override ImplantCreationUtilities.SplitConnectionDataModel SplitConnection(IConnection connection, Point3d point, Vector3d normal, double tolerance)
        {
            return ImplantCreationUtilities.SplitConnectionByAddingScrew(connection, point, normal, tolerance * 2, PastilleDiameter);
        }
    }
}
