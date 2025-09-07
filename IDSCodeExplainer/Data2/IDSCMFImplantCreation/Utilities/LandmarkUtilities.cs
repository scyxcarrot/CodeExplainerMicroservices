using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.Utilities
{
    public static class LandmarkUtilities
    {
        public static IMesh ScaleUpSurfaceForLandmark(IConsole console,
            IMesh supportMesh, double offsetDistance, IMesh surface)
        {
            var scaledSurface = GeometryTransformation.PerformMeshScalingOperation(console, surface, 1.2);

            var vertices = new List<IPoint3D>();
            foreach (var vertex in scaledSurface.Vertices)
            {
                var pt = ImplantCreationUtilities.EnsureVertexIsOnSameLevelAsThickness(console, supportMesh, new IDSPoint3D(vertex), offsetDistance);
                vertices.Add(pt);
            }

            var offsetted = OptimizeOffsetUtilities.OptimizeOffset(console, vertices, scaledSurface);

            return offsetted;
        }

        public static IMesh TransformLandmark(IConsole console, IMesh mesh, IPoint3D pastillePoint, IVector3D pastilleDirection,
            IPoint3D landmarkPoint, double distanceFromPastilleCenterToLandmarkCenter)
        {
            //transformation to initial
            //transformation to pastille's side
            var fromPlane = new IDSPlane(IDSPoint3D.Zero, IDSVector3D.XAxis);
            var toPlane = new IDSPlane(new IDSPoint3D(distanceFromPastilleCenterToLandmarkCenter, 0, 0), IDSVector3D.XAxis);
            var moveToSideTransform = GeometryTransformation.GetTransformationFromPlaneToPlane(console, fromPlane, toPlane);
            var initialPositionedMesh = GeometryTransformation.PerformMeshTransformOperation(console, mesh, moveToSideTransform);

            //transformation to pastille's center
            fromPlane = new IDSPlane(IDSPoint3D.Zero, IDSVector3D.ZAxis);
            toPlane = new IDSPlane(IDSPoint3D.Zero, pastilleDirection);
            var rotateTransform = GeometryTransformation.GetTransformationFromPlaneToPlane(console, fromPlane, toPlane);
            initialPositionedMesh = GeometryTransformation.PerformMeshTransformOperation(console, initialPositionedMesh, rotateTransform);

            fromPlane = new IDSPlane(IDSPoint3D.Zero, pastilleDirection);
            toPlane = new IDSPlane(pastillePoint, pastilleDirection);
            var translateTransform = GeometryTransformation.GetTransformationFromPlaneToPlane(console, fromPlane, toPlane);
            initialPositionedMesh = GeometryTransformation.PerformMeshTransformOperation(console, initialPositionedMesh, translateTransform);

            //transformation to landmark's point
            var xAxis = GeometryTransformation.PerformVectorTransformOperation(console, IDSVector3D.XAxis, rotateTransform);

            var projectedPoints = GeometryMath.ProjectPointsOnPlane(console, new List<IPoint3D> { landmarkPoint }, pastillePoint, pastilleDirection);
            var landmarkDirection = projectedPoints[0].Sub(pastillePoint);
            fromPlane = new IDSPlane(pastillePoint, xAxis);
            toPlane = new IDSPlane(pastillePoint, landmarkDirection);
            var moveTransform = GeometryTransformation.GetTransformationFromPlaneToPlane(console, fromPlane, toPlane);
            var movedToSideMesh = GeometryTransformation.PerformMeshTransformOperation(console, initialPositionedMesh, moveTransform);

            return new IDSMesh(movedToSideMesh);
        }
    }
}
