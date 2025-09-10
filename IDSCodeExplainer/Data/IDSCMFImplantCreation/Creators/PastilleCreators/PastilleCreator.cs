using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.CMFImplantCreation.Utilities;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace IDS.CMFImplantCreation.Creators
{
    internal class PastilleCreator : ComponentCreator
    {
        protected override string Name => "Pastille";

        public PastilleCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
            : base(console, componentInfo, configuration)
        {

        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is PastilleComponentInfo info))
            {
                throw new Exception("Invalid input!");
            }

            return CreatePastille(info);
        }

        private Task<IComponentResult> CreatePastille(PastilleComponentInfo info)
        {
            var timer = new Stopwatch();
            timer.Start();

            var component = new PastilleComponentResult
            {
                Id = _componentInfo.Id,
                IntermediateMeshes = new Dictionary<string, IMesh>(),
                IntermediateObjects = new Dictionary<string, object>(),
                ErrorMessages = new List<string>(),
                ComponentTimeTakenInSeconds = new Dictionary<string, double>()
            };

            try
            {
                try
                {
                    GeneratePastille(info, PastilleKeyNames.CreationAlgoPrimaryMethod, ref component);
                }
                catch
                {
                    GeneratePastille(info, PastilleKeyNames.CreationAlgoSecondaryMethod, ref component);
                }
            }
            catch (Exception e)
            {
                component.ErrorMessages.Add(e.Message);
            }

            timer.Stop();
            component.TimeTakenInSeconds = timer.ElapsedMilliseconds * 0.001;

            return Task.FromResult(component as IComponentResult);
        }

        private void GeneratePastille(PastilleComponentInfo info, string creationAlgoMethod, ref PastilleComponentResult component)
        {
            var componentFactory = new ComponentFactory();

            var intersectionCurveComponentInfo = info.ToActualComponentInfo<PastilleIntersectionCurveComponentInfo>();
            intersectionCurveComponentInfo.CreationAlgoMethod = creationAlgoMethod;
            var intersectionCurveCreator = componentFactory.CreateComponentCreator(_console, intersectionCurveComponentInfo, _configuration);
            var intersectionCurveComponentResult = intersectionCurveCreator.CreateComponentAsync().Result as PastilleIntersectionCurveComponentResult;
            TransferAllGeneralResults(intersectionCurveComponentResult, ref component);
            component.CreationAlgoMethod = intersectionCurveComponentResult.CreationAlgoMethod;
            if (intersectionCurveComponentResult.ErrorMessages.Any())
            {
                throw new Exception(intersectionCurveComponentResult.ErrorMessages.Last());
            }

            var intersectionCurve = intersectionCurveComponentResult.IntermediateObjects.GetLast(PastilleKeyNames.IntersectionCurveResult) as IDSCurve;

            IMesh extrusion = null;
            if (intersectionCurveComponentResult.CreationAlgoMethod == PastilleKeyNames.CreationAlgoPrimaryMethod)
            {
                var extrudeCylinderMesh = intersectionCurveComponentResult.IntermediateMeshes[PastilleKeyNames.CylinderExtrudeResult];

                var extrusionComponentInfo = info.ToActualComponentInfo<ExtrusionComponentInfo>();
                extrusionComponentInfo.ExtrudeCylinder = extrudeCylinderMesh;

                var extrusionCreator = componentFactory.CreateComponentCreator(_console, extrusionComponentInfo, _configuration);
                var extrusionComponentResult = extrusionCreator.CreateComponentAsync().Result as ExtrusionComponentResult;
                TransferAllGeneralResults(extrusionComponentResult, ref component);
                if (extrusionComponentResult.ErrorMessages.Any())
                {
                    throw new Exception(extrusionComponentResult.ErrorMessages.Last());
                }

                extrusion = extrusionComponentResult.IntermediateMeshes[PastilleKeyNames.ExtrusionResult];
            }

            var implantPastille = GenerateImplantPastilleComponent(info, ref component, intersectionCurve, extrusion);

            if (info.ComponentMeshes.Any())
            {
                var finalizationComponentInfo = new FinalizationComponentInfo(info);
                finalizationComponentInfo.ComponentMeshes.Add(implantPastille);
                var finalizationCreator = componentFactory.CreateComponentCreator(_console, finalizationComponentInfo, _configuration);
                var finalizationComponentResult = finalizationCreator.CreateComponentAsync().Result as FinalizationComponentResult;
                TransferAllGeneralResults(finalizationComponentResult, ref component);
                if (finalizationComponentResult.ErrorMessages.Any())
                {
                    throw new Exception(finalizationComponentResult.ErrorMessages.Last());
                }

                implantPastille = finalizationComponentResult.ComponentMesh;
            }

            component.ComponentMesh = implantPastille;
        }

        private void TransferAllGeneralResults(IComponentResult componentResult, ref PastilleComponentResult pastilleComponentResult)
        {
            pastilleComponentResult.ComponentMesh = componentResult.ComponentMesh;
            pastilleComponentResult.FinalComponentMesh = componentResult.FinalComponentMesh;

            foreach (var keyValuePair in componentResult.IntermediateMeshes)
            {
                pastilleComponentResult.IntermediateMeshes.Append(keyValuePair.Key, keyValuePair.Value);
            }

            foreach (var keyValuePair in componentResult.IntermediateObjects)
            {
                pastilleComponentResult.IntermediateObjects.Append(keyValuePair.Key, keyValuePair.Value);
            }

            foreach (var keyValuePair in componentResult.ComponentTimeTakenInSeconds)
            {
                if (pastilleComponentResult.ComponentTimeTakenInSeconds.ContainsKey(keyValuePair.Key))
                {
                    pastilleComponentResult.ComponentTimeTakenInSeconds.Remove(keyValuePair.Key);
                }

                pastilleComponentResult.ComponentTimeTakenInSeconds.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        private IMesh GenerateImplantPastilleComponent(PastilleComponentInfo info, ref PastilleComponentResult component, ICurve interCurve, IMesh extrusion)
        {
            var pastille = info.ToDataModel(_configuration.GetPastilleConfiguration(info.ScrewType));

            var individualImplantParams = _configuration.GetIndividualImplantParameter();
            var wrapRatio = individualImplantParams.WrapOperationOffsetInDistanceRatio;

            double compensatePastille;
            double wrapValue;
            ImplantCreationUtilities.CalculatePastilleParameters(pastille, wrapRatio, out wrapValue, out compensatePastille);

            var finalWrapOffset = wrapValue * individualImplantParams.WrapOperationOffsetInDistanceRatio;
            var thickness = pastille.Thickness;
            var offsetDistance = (thickness - finalWrapOffset) / 2;
            var offsetDistanceUpper = thickness - finalWrapOffset;
            if (offsetDistanceUpper < 0.00)
            {
                throw new Exception("Implant Pastille thickness and diameter ratio invalid.");
            }

            if (!interCurve.IsClosed())
            {
                throw new Exception(ErrorUtilities.ImplantCreationErrorCurveNotClosed);
            }

            var doUniformOffset = extrusion == null;

            var componentFactory = new ComponentFactory();

            var patchComponentInfo = info.ToActualComponentInfo<PatchComponentInfo>();
            patchComponentInfo.IntersectionCurve = interCurve;
            patchComponentInfo.DoUniformOffset = doUniformOffset;
            patchComponentInfo.OffsetDistanceUpper = offsetDistanceUpper;
            patchComponentInfo.OffsetDistance = offsetDistance;

            var patchCreator = componentFactory.CreateComponentCreator(_console, patchComponentInfo, _configuration);
            var patchComponentResult = patchCreator.CreateComponentAsync().Result as PatchComponentResult;
            TransferAllGeneralResults(patchComponentResult, ref component);
            if (patchComponentResult.ErrorMessages.Any())
            {
                throw new Exception(patchComponentResult.ErrorMessages.Last());
            }

            var top = patchComponentResult.IntermediateMeshes[PastilleKeyNames.OffsetTopResult];
            var bottom = patchComponentResult.IntermediateMeshes[PastilleKeyNames.OffsetBottomResult];

            var offsetMesh = OptimizeOffsetForPastille(info, ref component, extrusion, ref top, ref bottom);

            var pastilleMeshes = new List<IMesh>();
            pastilleMeshes.Add(offsetMesh);

            //Add Screw Stamp Imprint Smarties /////////////////////////////////////////////////////////////
            var screwStampImprintComponentInfo = info.ToActualComponentInfo<ScrewStampImprintComponentInfo>();
            var screwStampImprintCreator = componentFactory.CreateComponentCreator(_console, screwStampImprintComponentInfo, _configuration);
            var screwStampImprintComponentResult = screwStampImprintCreator.CreateComponentAsync().Result as ScrewStampImprintComponentResult;
            TransferAllGeneralResults(screwStampImprintComponentResult, ref component);
            if (screwStampImprintComponentResult.ErrorMessages.Any())
            {
                //warning message. Not an error. Recommend to use Console to write warning messages.
            }

            var stampImprintShapeMesh = screwStampImprintComponentResult.IntermediateMeshes[PastilleKeyNames.StampImprintResult];
            if (stampImprintShapeMesh != null)
            {
                pastilleMeshes.Add(stampImprintShapeMesh);
            }

            var smallestDetail = individualImplantParams.WrapOperationSmallestDetails;
            var gapClosingDistance = individualImplantParams.WrapOperationGapClosingDistance;
            var wrappedMesh = WrapOffset(MeshUtilitiesV2.AppendMeshes(pastilleMeshes), smallestDetail, gapClosingDistance, finalWrapOffset);
            return wrappedMesh;
        }

        private IMesh OptimizeOffsetForPastille(PastilleComponentInfo info, ref PastilleComponentResult component, IMesh extrusion,
            ref IMesh top, ref IMesh bottom)
        {
            if (extrusion != null)
            {
                return BuildSolidMesh(info, ref component, extrusion, ref top, ref bottom);
            }
            else
            {
                var componentFactory = new ComponentFactory();

                var stitchMeshComponentInfo = info.ToActualComponentInfo<StitchMeshComponentInfo>();
                stitchMeshComponentInfo.TopMesh = top;
                stitchMeshComponentInfo.BottomMesh = bottom;

                var stitchMeshCreator = componentFactory.CreateComponentCreator(_console, stitchMeshComponentInfo, _configuration);
                var stitchMeshComponentResult = stitchMeshCreator.CreateComponentAsync().Result as StitchMeshComponentResult;
                TransferAllGeneralResults(stitchMeshComponentResult, ref component);
                if (stitchMeshComponentResult.ErrorMessages.Any())
                {
                    throw new Exception(stitchMeshComponentResult.ErrorMessages.Last());
                }

                var offset = stitchMeshComponentResult.IntermediateMeshes[PastilleKeyNames.OffsetStitchMeshResult];

                return offset;
            }
        }

        private IMesh BuildSolidMesh(PastilleComponentInfo info, ref PastilleComponentResult component, IMesh extrusion, ref IMesh top, ref IMesh bottom)
        {
            var componentFactory = new ComponentFactory();

            var solidMeshComponentInfo = info.ToActualComponentInfo<SolidMeshComponentInfo>();
            solidMeshComponentInfo.ExtrusionMesh = extrusion;
            solidMeshComponentInfo.TopMesh = top;
            solidMeshComponentInfo.BottomMesh = bottom;

            var solidMeshCreator = componentFactory.CreateComponentCreator(_console, solidMeshComponentInfo, _configuration);
            var solidMeshComponentResult = solidMeshCreator.CreateComponentAsync().Result as SolidMeshComponentResult;
            TransferAllGeneralResults(solidMeshComponentResult, ref component);
            if (solidMeshComponentResult.ErrorMessages.Any())
            {
                throw new Exception(solidMeshComponentResult.ErrorMessages.Last());
            }

            top = solidMeshComponentResult.IntermediateMeshes[PastilleKeyNames.TopSolidMeshResult];
            bottom = solidMeshComponentResult.IntermediateMeshes[PastilleKeyNames.BottomSolidMeshResult];
            var offsetMesh = solidMeshComponentResult.IntermediateMeshes[PastilleKeyNames.OffsetSolidMeshResult];

            return offsetMesh;
        }

        private IMesh WrapOffset(IMesh offsetMesh, double smallestDetail, double gapClosingDistance, double wrapValue)
        {
            if (!WrapV2.PerformWrap(_console, new IMesh[] { offsetMesh }, smallestDetail, gapClosingDistance, wrapValue, false, false, false, false, out var wrappedMesh))
            {
                throw new Exception("wrapped plate tube failed.");
            }
            if (wrappedMesh == null)
            {
                throw new Exception("wrapped implant plate failed.");
            }

            return wrappedMesh;
        }
    }
}
