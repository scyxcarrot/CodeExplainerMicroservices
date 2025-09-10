using IDS.CMF.V2.DataModel;
using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.CMFImplantCreation.Utilities;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IDS.CMFImplantCreation.Creators
{
    internal class TriangleLandmarkCreator : ComponentCreator
    {
        protected override string Name => "TriangleLandmark";

        public TriangleLandmarkCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
            : base(console, componentInfo, configuration)
        {
            componentInfo.NeedToFinalize = false;
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is LandmarkComponentInfo info) || info.Type != LandmarkType.Triangle)
            {
                throw new Exception("Invalid input!");
            }

            return CreateTriangleLandmark(info);
        }

        private Task<IComponentResult> CreateTriangleLandmark(LandmarkComponentInfo info)
        {
            var timer = new Stopwatch();
            timer.Start();

            var component = new LandmarkComponentResult
            {
                Id = _componentInfo.Id,
                IntermediateMeshes = new Dictionary<string, IMesh>(),
                IntermediateObjects = new Dictionary<string, object>(),
                ErrorMessages = new List<string>()
            };

            try
            {
                GenerateTriangleLandmark(info, ref component);
            }
            catch (Exception e)
            {
                component.ErrorMessages.Add(e.Message);
            }

            timer.Stop();
            component.TimeTakenInSeconds = timer.ElapsedMilliseconds * 0.001;

            return Task.FromResult(component as IComponentResult);
        }

        private void GenerateTriangleLandmark(LandmarkComponentInfo info, ref LandmarkComponentResult component)
        {
            var landmarkImplantParams = _configuration.GetLandmarkImplantParameter(); 
            var wrapOffset = info.PastilleThickness * 0.25; //Y

            //Step 1 - Create a base of the landmark for Step 2 to take place.
            var landmarkBaseMesh = CreateBaseLandmarkMesh(landmarkImplantParams, info, wrapOffset);

            //Step 2 - To maintain Shape, create the one with actual size compensated by the offset.
            var landmarkMesh = CreateLandmarkMesh(landmarkImplantParams, info, wrapOffset);

            component.IntermediateMeshes.Add(LandmarkKeyNames.LandmarkBaseMeshResult, landmarkBaseMesh);
            component.IntermediateMeshes.Add(LandmarkKeyNames.LandmarkMeshResult, landmarkMesh);
        }

        private IMesh CreateBaseLandmarkMesh(LandmarkImplantParams landmarkImplantParams, LandmarkComponentInfo landmarkInfo, double intendedWrapOffset)
        {
            var radius = landmarkInfo.PastilleDiameter * 0.5;

            var baseLandmark = CreateTriangleLandmark(landmarkInfo.PastilleThickness * 4, radius * 1.2, intendedWrapOffset, landmarkImplantParams);

            return TransformTriangleLandmark(baseLandmark, landmarkInfo.PastilleLocation, landmarkInfo.PastilleDirection, landmarkInfo.Point, 
                landmarkInfo.PastilleDiameter / 2);
        }

        private IMesh CreateLandmarkMesh(LandmarkImplantParams landmarkImplantParams, LandmarkComponentInfo landmarkInfo, double wrapOffsetValue)
        {
            var radius = landmarkInfo.PastilleDiameter * 0.5;

            var landmark = CreateTriangleLandmark(landmarkInfo.PastilleDiameter, radius, wrapOffsetValue, landmarkImplantParams); //increase thickness (extrude) in order to make sure intersection happens with support mesh
            
            return TransformTriangleLandmark(landmark, landmarkInfo.PastilleLocation, landmarkInfo.PastilleDirection, landmarkInfo.Point, 
                landmarkInfo.PastilleDiameter / 2);
        }

        //TriangleHeight is towards XAxis and TriangleBaseLength is towards YAxis
        private IMesh CreateTriangleLandmark(double thickness, double pastilleRadius, double wrapOffset, LandmarkImplantParams landmarkImplantParams)
        {
            var compensatedPastilleRadius = pastilleRadius - wrapOffset;

            //take triangleBaseLength as a, use formula: a^2 = b^2 + c^2 where b == c
            var triangleBaseLength = Math.Sqrt(2 * Math.Pow(compensatedPastilleRadius, 2));
            var triangleHeight = triangleBaseLength / 2;
            var compensatedHeight = triangleHeight * landmarkImplantParams.TriangleHeightRatioWithDefault;

            var point1 = new IDSPoint3D(-compensatedHeight / 2, -triangleBaseLength / 2, 0.0);
            var point2 = new IDSPoint3D(compensatedHeight / 2, 0.0, 0.0);
            var point3 = new IDSPoint3D(-compensatedHeight / 2, triangleBaseLength / 2, 0.0);

            var points = new List<IPoint3D>();
            points.Add(point1);
            points.Add(point2);
            points.Add(point3);
            points.Add(point1);

            var curve = new IDSCurve(points);
 
            var actualCenter = new IDSPoint3D(-pastilleRadius + (triangleHeight * 1.5), 0.0, 0.0);

            var triangleMesh = Curves.GeneratePolygon(_console, curve,
                    actualCenter,
                    thickness);

            return triangleMesh;
        }

        private IMesh TransformTriangleLandmark(IMesh mesh, IPoint3D pastillePoint, IVector3D pastilleDirection, IPoint3D landmarkPoint, double pastilleRadius)
        {
            var distanceFromPastilleCenterToLandmarkCenter = pastilleRadius;

            return LandmarkUtilities.TransformLandmark(_console, mesh, pastillePoint, pastilleDirection, landmarkPoint, 
                distanceFromPastilleCenterToLandmarkCenter);
        }
    }
}
