using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.CMFImplantCreation.Utilities;
using IDS.Core.V2.Extensions;
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
    internal class ScrewStampImprintCreator : ComponentCreator
    {
        protected override string Name => "ScrewStampImprint";

        public ScrewStampImprintCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration) : base(console, componentInfo, configuration)
        {
            componentInfo.NeedToFinalize = false;
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            var info = _componentInfo as ScrewStampImprintComponentInfo;

            if (info == null)
            {
                throw new Exception("Invalid input!");
            }

            return CreateScrewStampImprint(info);
        }

        private Task<IComponentResult> CreateScrewStampImprint(ScrewStampImprintComponentInfo info)
        {
            var timer = new Stopwatch();
            timer.Start();

            var component = new ScrewStampImprintComponentResult()
            {
                Id = _componentInfo.Id,
                IntermediateMeshes = new Dictionary<string, IMesh>(),
                IntermediateObjects = new Dictionary<string, object>(),
                ErrorMessages = new List<string>(),
                ComponentTimeTakenInSeconds = new Dictionary<string, double>()
            };

            try
            {
                var model = info.ToDataModel(_configuration.GetPastilleConfiguration(info.ScrewType));
                
                var shapeOffset = model.StampImprintShapeOffset;
                var shapeWidth = model.StampImprintShapeWidth;
                var shapeHeight = model.StampImprintShapeHeight;
                var shapeSectionHeightRatio = model.StampImprintShapeSectionHeightRatio;
                var shapeCreationMaxPastilleThickness = model.StampImprintShapeCreationMaxPastilleThickness;

                component.IntermediateObjects.Add(PastilleKeyNames.ShapeOffsetResult, shapeOffset);
                component.IntermediateObjects.Add(PastilleKeyNames.ShapeWidthResult, shapeWidth);
                component.IntermediateObjects.Add(PastilleKeyNames.ShapeHeightResult, shapeHeight);
                component.IntermediateObjects.Add(PastilleKeyNames.ShapeSectionHeightRatioResult, shapeSectionHeightRatio);
                component.IntermediateObjects.Add(PastilleKeyNames.ShapeCreationMaxPastilleThicknessResult, shapeCreationMaxPastilleThickness);

                const double epsilon = 0.001;
                component.IntermediateMeshes.Add(PastilleKeyNames.StampImprintResult, null);

                if (model.Thickness <= shapeCreationMaxPastilleThickness)
                {
                    if (shapeWidth / 2 > epsilon && shapeHeight > epsilon)
                    {
                        var stampImprintShapeMesh = GenerateStampImprintShapeMesh(_console,
                            info.ScrewHeadPoint, info.ScrewDirection,
                            shapeOffset, shapeWidth, shapeHeight, shapeSectionHeightRatio);

                        component.IntermediateMeshes[PastilleKeyNames.StampImprintResult] = stampImprintShapeMesh;
                    }
                    else
                    {
                        component.ErrorMessages.Add(
                            $"Implant Screw {info.ScrewType}'s Stamp Imprint Shape has Width or Height with 0 value, hence it is skipped");
                    }
                }
                else
                {
                    component.ErrorMessages.Add(
                        $"Pastille thickness {model.Thickness} is more than the max allowable thickness of {shapeCreationMaxPastilleThickness}, hence it is skipped");
                }
            }
            catch (Exception e)
            {
                component.ErrorMessages.Add(e.Message);
            }

            timer.Stop();
            component.TimeTakenInSeconds = timer.ElapsedMilliseconds * 0.001;
            component.ComponentTimeTakenInSeconds.Add(Name, component.TimeTakenInSeconds);

            return Task.FromResult(component as IComponentResult);
        }

        private static IMesh GenerateStampImprintShapeMesh(IConsole console,
            IPoint3D screwHeadPoint, IVector3D screwDirection,
            double shapeOffset, double shapeWidth, double shapeHeight, double sectionHeightRatio)
        {
            var shapeHalfWidth = shapeWidth / 2;
            var scaleRatio = shapeHeight / shapeWidth;

            var headPointTranslation = screwDirection.Mul(shapeOffset + (shapeHeight / 2));
            var translatedHeadPoint = screwHeadPoint.Add(headPointTranslation);

            var squishTransform = IDSTransform.Identity;
            squishTransform.M22 = 1 * scaleRatio;

            var stampImprintShapeMesh = Primitives.GenerateSphere(console, IDSPoint3D.Zero, shapeHalfWidth);
            stampImprintShapeMesh = GeometryTransformation.PerformMeshTransformOperation(console, stampImprintShapeMesh, squishTransform);
            if (sectionHeightRatio > 0 && sectionHeightRatio <= 1)
            {
                var topPoint = PointUtilities.FindFurthermostPointAlongVector(stampImprintShapeMesh.Vertices,
                    -IDSVector3D.ZAxis);

                var translatedTopPoint = topPoint.Add(IDSVector3D.ZAxis.Mul(shapeHeight * sectionHeightRatio));

                var cylinderMesh = Primitives.GenerateCylinderWithLocationAsBase(console,
                    translatedTopPoint,
                    IDSVector3D.ZAxis,
                    shapeHalfWidth * 1.5,
                    shapeHeight * 1.5);

                var topSideSmartie = BooleansV2.PerformBooleanSubtraction(console, stampImprintShapeMesh, cylinderMesh);
                stampImprintShapeMesh = new IDSMesh(topSideSmartie);
            }

            var fromPlane = new IDSPlane(IDSPoint3D.Zero, IDSVector3D.ZAxis);
            var toPlane = new IDSPlane(IDSPoint3D.Zero, screwDirection);
            var rotateTransform = GeometryTransformation.GetTransformationFromPlaneToPlane(console, fromPlane, toPlane);
            stampImprintShapeMesh = GeometryTransformation.PerformMeshTransformOperation(console, stampImprintShapeMesh, rotateTransform);

            fromPlane = new IDSPlane(IDSPoint3D.Zero, IDSVector3D.ZAxis);
            toPlane = new IDSPlane(translatedHeadPoint, IDSVector3D.ZAxis);
            var translateTransform = GeometryTransformation.GetTransformationFromPlaneToPlane(console, fromPlane, toPlane);
            stampImprintShapeMesh = GeometryTransformation.PerformMeshTransformOperation(console, stampImprintShapeMesh, translateTransform);

            return stampImprintShapeMesh;
        }
    }
}
