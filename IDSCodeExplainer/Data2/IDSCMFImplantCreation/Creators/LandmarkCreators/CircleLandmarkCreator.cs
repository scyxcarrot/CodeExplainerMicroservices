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
    internal class CircleLandmarkCreator : ComponentCreator
    {
        protected override string Name => "CircleLandmark";

        public CircleLandmarkCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
            : base(console, componentInfo, configuration)
        {
            componentInfo.NeedToFinalize = false;
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is LandmarkComponentInfo info) || info.Type != LandmarkType.Circle)
            {
                throw new Exception("Invalid input!");
            }

            return CreateCircleLandmark(info);
        }

        private Task<IComponentResult> CreateCircleLandmark(LandmarkComponentInfo info)
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
                GenerateCircleLandmark(info, ref component);
            }
            catch (Exception e)
            {
                component.ErrorMessages.Add(e.Message);
            }

            timer.Stop();
            component.TimeTakenInSeconds = timer.ElapsedMilliseconds * 0.001;

            return Task.FromResult(component as IComponentResult);
        }

        private void GenerateCircleLandmark(LandmarkComponentInfo info, ref LandmarkComponentResult component)
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

            var baseLandmark = CreateCircleLandmark(landmarkInfo.PastilleThickness * 4, radius * 1.2, intendedWrapOffset, landmarkImplantParams);

            return TransformCircleLandmark(baseLandmark, landmarkInfo.PastilleLocation, landmarkInfo.PastilleDirection, landmarkInfo.Point, 
                landmarkInfo.PastilleDiameter / 2, landmarkImplantParams);
        }

        private IMesh CreateLandmarkMesh(LandmarkImplantParams landmarkImplantParams, LandmarkComponentInfo landmarkInfo, double wrapOffsetValue)
        {
            var radius = landmarkInfo.PastilleDiameter * 0.5;
            var wrapOffset = wrapOffsetValue;

            var landmark = CreateCircleLandmark(landmarkInfo.PastilleDiameter, radius, wrapOffset, landmarkImplantParams); //increase thickness (extrude) in order to make sure intersection happens with support mesh
            
            return TransformCircleLandmark(landmark, landmarkInfo.PastilleLocation, landmarkInfo.PastilleDirection, landmarkInfo.Point, 
                landmarkInfo.PastilleDiameter / 2, landmarkImplantParams);
        }

        private IMesh CreateCircleLandmark(double thickness, double pastilleRadius, double wrapOffset, LandmarkImplantParams landmarkImplantParams)
        {
            var baseCircleCenter = new IDSPoint3D(0, 0, -thickness / 2);
            var landmarkRadius = Math.Abs((pastilleRadius * landmarkImplantParams.CircleRadiusRatioWithPastilleRadius) / 2 - wrapOffset);
            var cylinderMesh = Primitives.GenerateCylinderWithLocationAsBase(_console,
                    baseCircleCenter,
                    IDSVector3D.ZAxis,
                    landmarkRadius,
                    thickness);
            return cylinderMesh;
        }

        private IMesh TransformCircleLandmark(IMesh mesh, IPoint3D pastillePoint, IVector3D pastilleDirection, IPoint3D landmarkPoint, double pastilleRadius, LandmarkImplantParams landmarkImplantParams)
        {
            var distanceFromPastilleCenterToLandmarkCenter = pastilleRadius * landmarkImplantParams.CircleCenterRatioWithPastilleRadius;

            return LandmarkUtilities.TransformLandmark(_console, mesh, pastillePoint, pastilleDirection, landmarkPoint, 
                distanceFromPastilleCenterToLandmarkCenter);
        }
    }
}
