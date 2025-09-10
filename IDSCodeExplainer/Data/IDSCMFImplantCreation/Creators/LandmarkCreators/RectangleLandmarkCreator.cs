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
    internal class RectangleLandmarkCreator : ComponentCreator
    {
        protected override string Name => "RectangleLandmark";

        public RectangleLandmarkCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
            : base(console, componentInfo, configuration)
        {
            componentInfo.NeedToFinalize = false;
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is LandmarkComponentInfo info) || info.Type != LandmarkType.Rectangle)
            {
                throw new Exception("Invalid input!");
            }

            return CreateRectangleLandmark(info);
        }

        private Task<IComponentResult> CreateRectangleLandmark(LandmarkComponentInfo info)
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
                GenerateRectangleLandmark(info, ref component);
            }
            catch (Exception e)
            {
                component.ErrorMessages.Add(e.Message);
            }

            timer.Stop();
            component.TimeTakenInSeconds = timer.ElapsedMilliseconds * 0.001;

            return Task.FromResult(component as IComponentResult);
        }

        private void GenerateRectangleLandmark(LandmarkComponentInfo info, ref LandmarkComponentResult component)
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

            var baseLandmark = CreateRectangleLandmark(landmarkInfo.PastilleThickness * 4, radius * 1.2, intendedWrapOffset, landmarkImplantParams);

            return TransformRectangleLandmark(baseLandmark, landmarkInfo.PastilleLocation, landmarkInfo.PastilleDirection, landmarkInfo.Point, 
                landmarkInfo.PastilleDiameter / 2, landmarkImplantParams);
        }

        private IMesh CreateLandmarkMesh(LandmarkImplantParams landmarkImplantParams, LandmarkComponentInfo landmarkInfo, double wrapOffsetValue)
        {
            var radius = landmarkInfo.PastilleDiameter * 0.5;
            var wrapOffset = wrapOffsetValue;

            var landmark = CreateRectangleLandmark(landmarkInfo.PastilleDiameter, radius, wrapOffset, landmarkImplantParams); //increase thickness (extrude) in order to make sure intersection happens with support mesh
            
            return TransformRectangleLandmark(landmark, landmarkInfo.PastilleLocation, landmarkInfo.PastilleDirection, landmarkInfo.Point, 
                landmarkInfo.PastilleDiameter / 2, landmarkImplantParams);
        }

        private IMesh CreateRectangleLandmark(double thickness, double pastilleRadius, double wrapOffset, LandmarkImplantParams landmarkImplantParams)
        {
            var center = IDSPoint3D.Zero; 
            var landmarkWidth = GetRectangleWidth(pastilleRadius, wrapOffset, landmarkImplantParams);
            var landmarkHeight = landmarkWidth;
            var landmarkDepth = thickness;

            var rectangleMesh = Primitives.GenerateBox(_console,
                    center,
                    landmarkWidth,
                    landmarkHeight,
                    landmarkDepth);
            return rectangleMesh;
        }

        private IMesh TransformRectangleLandmark(IMesh mesh, IPoint3D pastillePoint, IVector3D pastilleDirection, IPoint3D landmarkPoint, double pastilleRadius, LandmarkImplantParams landmarkImplantParams)
        {
            var landmarkWidth = GetRectangleWidth(pastilleRadius, 0.0, landmarkImplantParams);
            var distanceFromPastilleCenterToLandmarkCenter = pastilleRadius - (landmarkWidth / 2) + landmarkImplantParams.SquareExtensionFromPastilleCircumference;

            return LandmarkUtilities.TransformLandmark(_console, mesh, pastillePoint, pastilleDirection, landmarkPoint, 
                distanceFromPastilleCenterToLandmarkCenter);
        }

        private double GetRectangleWidth(double pastilleRadius, double wrapOffset, LandmarkImplantParams landmarkImplantParams)
        {
            var landmarkWidth = (pastilleRadius * landmarkImplantParams.SquareWidthRatioWithPastilleRadius) - 2 * wrapOffset;
            return landmarkWidth;
        }
    }
}
