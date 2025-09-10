using IDS.CMF.Constants;
using IDS.CMF.Factory;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;
using Plane = Rhino.Geometry.Plane;

namespace IDS.CMF.Utilities
{
    public static class GuideBridgeUtilities
    {
        public static Plane CreateBridgeCoordinateSystem(Point3d bridgeStartPt, Point3d bridgeEndPt, Vector3d bridgeUpDir)
        {
            var midPt = new Point3d((bridgeStartPt + bridgeEndPt) / 2);
            var mainAxis = bridgeStartPt - bridgeEndPt;
            mainAxis.Unitize();
            var upAxis = bridgeUpDir;
            upAxis.Unitize();
            var otherAxis = Vector3d.CrossProduct(upAxis, mainAxis);
            otherAxis.Unitize();
            return new Plane(midPt, mainAxis, otherAxis);
        }

        public static List<Point3d> GetStartEndPoints(Brep bridgeBrep)
        {
            var startEndPtList = new List<Point3d>();
            foreach (var vertex in bridgeBrep.Vertices)
            {
                if (!bridgeBrep.Edges.Any(e => e.StartVertex.Location.EpsilonEquals(vertex.Location, 0.001) ||
                                               e.EndVertex.Location.EpsilonEquals(vertex.Location, 0.001)))
                {
                    startEndPtList.Add(vertex.Location);
                }
            }
            return startEndPtList;
        }

        public static Brep GetCompensatedGuideBridgeForLightweight(Brep bridgeBrep, Plane coordinateSystem, double lightweightSegmentRadius)
        {
            var startEndPtList = GetStartEndPoints(bridgeBrep);
            var startPoint = startEndPtList.First();
            var endPoint = startEndPtList.Last();

            bridgeBrep.UserDictionary.TryGetString(AttributeKeys.KeyGuideBridgeType, out var bridgeType);
            bridgeBrep.UserDictionary.TryGetBool(AttributeKeys.KeyGuideBridgeGenio, out var bridgeGenio);
            bridgeBrep.UserDictionary.TryGetDouble(AttributeKeys.KeyGuideBridgeDiameter, out var bridgeDiameter);
            var guideBridgeBrepFactory = new GuideBridgeBrepFactory(bridgeType == "" ? null : bridgeType, bridgeGenio);
            var compensatedBridgeForLightweight = guideBridgeBrepFactory.CreateCompensatedGuideBridgeForLightweight(startPoint, endPoint, coordinateSystem.ZAxis, lightweightSegmentRadius, bridgeDiameter);
            return compensatedBridgeForLightweight;
        }

        public static double GetGuideBridgeRadius(Brep bridgeBrep, Plane coordinateSystem)
        {
            var startEndPtList = GetStartEndPoints(bridgeBrep);
            var startPoint = startEndPtList.First();
            var endPoint = startEndPtList.Last();

            return (startPoint - endPoint).Length / 2;
        }

        public static Mesh GenerateGuideBridgeWithLightweightFromBrep(Brep bridgeBrep, Plane coordinateSystem, double lightweightSegmentRadius, double lightweightFractionalTriangleEdgeLength, double compensation = 0.0)
        {
            // For octagonal bridges, a lower compensation on the brep would yield better results
            // if compensation value is unassigned, we will just use lightweightSegmentRadius for creating compensatedBridge
            var compensatedBridgeForLightweight = GetCompensatedGuideBridgeForLightweight(bridgeBrep, coordinateSystem, compensation != 0.0 ? compensation : lightweightSegmentRadius);

            var lwBridge = MeshFromPolyline.PerformMeshFromPolyline(compensatedBridgeForLightweight, lightweightSegmentRadius, lightweightFractionalTriangleEdgeLength);
            return lwBridge;
        }
    }
}
