using System.Collections.Generic;
using System.Linq;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;

namespace IDS.Glenius.Visualization
{
    public class CameraViewPresets
    {
        public double CameraDistance { get; set; } = 580.0;

        protected readonly AnatomicalMeasurements AnatomicalInfo;
        protected readonly RhinoViewport Viewport;
        protected readonly bool IsLeft;

        public CameraViewPresets(AnatomicalMeasurements anatomicalInfo, RhinoViewport viewport, bool isLeft)
        {
            AnatomicalInfo = anatomicalInfo;
            Viewport = viewport;
            IsLeft = isLeft;
        }

        protected void PositionCamera(Plane cameraLookAtPlane, double cameraDistance, Vector3d cameraUpVector)
        {
            Viewport.SetCameraLocations(Point3d.Origin, (Point3d)Vector3d.YAxis); // reset

            var camPosition = cameraLookAtPlane.Origin + (cameraLookAtPlane.Normal * cameraDistance);
            Viewport.SetCameraLocations(cameraLookAtPlane.Origin, camPosition);
            Viewport.CameraUp = cameraUpVector;
            Viewport.ZoomExtents();
        }

        protected Plane CreatePlaneInvertedNormal(Plane plane)
        {
            return new Plane(plane.Origin, -plane.Normal);
        }

        public void SetCameraToSuperiorView()
        {
            PositionCamera(AnatomicalInfo.PlAxial, CameraDistance, IsLeft? -AnatomicalInfo.PlCoronal.Normal : AnatomicalInfo.PlCoronal.Normal);
        }

        public void SetCameraToAnteriorView()
        {
            PositionCamera(IsLeft ? AnatomicalInfo.PlCoronal : CreatePlaneInvertedNormal(AnatomicalInfo.PlCoronal),
                CameraDistance, AnatomicalInfo.PlAxial.Normal);
        }

        public void SetCameraToLateralView()
        {
            PositionCamera(AnatomicalInfo.PlSagittal, CameraDistance, AnatomicalInfo.PlAxial.Normal);
        }

        public void SetCameraToPosteriorView()
        {
            PositionCamera(IsLeft? CreatePlaneInvertedNormal(AnatomicalInfo.PlCoronal) : AnatomicalInfo.PlCoronal,
                CameraDistance, AnatomicalInfo.PlAxial.Normal);
        }

        public void SetCameraToAnteroLateralView()
        {
            var anteriorViewPlane = IsLeft ? AnatomicalInfo.PlCoronal : CreatePlaneInvertedNormal(AnatomicalInfo.PlCoronal);
            var lateralViewPlane = AnatomicalInfo.PlSagittal;
            var anterolateralViewPlane = new Plane(anteriorViewPlane.Origin, (anteriorViewPlane.Normal + lateralViewPlane.Normal) / 2);

            PositionCamera(anterolateralViewPlane, CameraDistance, AnatomicalInfo.PlAxial.Normal);
        }

        public void SetCameraToPosteroLateralView()
        {
            var posteriorViewPlane = IsLeft ? CreatePlaneInvertedNormal(AnatomicalInfo.PlCoronal) : AnatomicalInfo.PlCoronal;
            var lateralViewPlane = AnatomicalInfo.PlSagittal;
            var posterolateralViewPlane = new Plane(posteriorViewPlane.Origin, (posteriorViewPlane.Normal + lateralViewPlane.Normal) / 2);

            PositionCamera(posterolateralViewPlane, CameraDistance, AnatomicalInfo.PlAxial.Normal);
        }

        public void SetCameraToMedialView()
        {
            PositionCamera(CreatePlaneInvertedNormal(AnatomicalInfo.PlSagittal), CameraDistance, AnatomicalInfo.PlAxial.Normal);
        }

        public void SetCameraToInferiorView()
        {
            PositionCamera(CreatePlaneInvertedNormal(AnatomicalInfo.PlAxial), CameraDistance, IsLeft ? AnatomicalInfo.PlCoronal.Normal : -AnatomicalInfo.PlCoronal.Normal);
        }
    }
}
