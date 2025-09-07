using IDS.CMF.Utilities;
using IDS.Interface.Implant;
using Rhino.Geometry;

namespace IDS.PICMF.DrawingAction
{
    public class InsertControlPointAction : InsertDotAction
    {
        protected override ImplantCreationUtilities.SplitConnectionDataModel SplitConnection(IConnection connection, Point3d point, Vector3d normal, double tolerance)
        {
            return ImplantCreationUtilities.SplitConnectionByAddingControlPoint(connection, point, normal, tolerance * 2);
        }
    }
}
