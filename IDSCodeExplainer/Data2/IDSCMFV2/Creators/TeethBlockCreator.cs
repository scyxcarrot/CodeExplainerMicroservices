using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using System.Linq;

namespace IDS.CMF.V2.Creators
{

    public class TeethBlockCreator
    {
        private TeethBlockCreatorInput _teethBlockCreatorInput { get; }

        public TeethBlockCreatorDataModel Output { get; set; }

        public TeethBlockCreator(
            TeethBlockCreatorInput teethBlockCreatorInput,
            TeethBlockCreatorDataModel teethBlockCreatorDataModel)
        {
            _teethBlockCreatorInput = teethBlockCreatorInput;
            // Create a copy so that we dont affect the input data model
            Output = new TeethBlockCreatorDataModel(teethBlockCreatorDataModel);
        }

        public void CreateTeethBlock()
        {
            CreateTeethBlockRoi(out var wrappedLimitingSurfaceExtrusion, out var averageNormal);
            CreateFinalSupport(wrappedLimitingSurfaceExtrusion);
            CreateFinalTeethBlock(averageNormal);
        }

        private void CreateFinalTeethBlock(IVector3D averageNormal)
        {
            CreateReinforcementExtrusion(averageNormal.Invert());
            CreateFinalSupportWrapped();
            CreateTeethBlock(averageNormal);
        }

        private void CreateTeethBlock(IVector3D averageNormal)
        {
            CreateTeethBaseExtrusion(averageNormal);

            BooleansV2.PerformBooleanUnion(
                _teethBlockCreatorInput.Console, 
                out var teethBaseExtrusionAppended,
                Output.TeethBaseRegionIdAndExtrusionMap.Values.ToArray());
            var teethBlockRaw = BooleansV2.PerformBooleanIntersection(
                _teethBlockCreatorInput.Console,
                Output.FinalSupportWrapped,
                teethBaseExtrusionAppended);
            var bracketExtrusionExtra = CreateBracketExtrusionWithDistance(
                averageNormal, 31);
            // union all the items to subtract from teethBlockRaw
            BooleansV2.PerformBooleanUnion(
                _teethBlockCreatorInput.Console,
                out var finalSupportAndBracketExtrusion,
                Output.FinalSupport, bracketExtrusionExtra
            );
            var teethBlockSubtracted = BooleansV2.PerformBooleanSubtraction(
                _teethBlockCreatorInput.Console,
                teethBlockRaw,
                finalSupportAndBracketExtrusion);
            var finalTeethBlock = FinalizeToTeethBlock(teethBlockSubtracted);

            Output.TeethBlock = finalTeethBlock;
        }

        private IMesh FinalizeToTeethBlock(
            IMesh finalSubtraction)
        {
            var offsetIdsMesh = MeshDesignV2.Offset(
                _teethBlockCreatorInput.Console,
                finalSubtraction,
                -0.15,
                false,
                0.05,
                true);

            var fixIdsMesh = AutoFixV2.RemoveNoiseShells(_teethBlockCreatorInput.Console, offsetIdsMesh);

            WrapV2.PerformWrap(
                _teethBlockCreatorInput.Console,
                new[] { fixIdsMesh },
                0.1,
                0.5,
                0.16,
                false,
                true,
                false,
                false,
                out var wrappedIdsMesh);

            var finalizedIdsMesh = BooleansV2.PerformBooleanIntersection(
                _teethBlockCreatorInput.Console,
                wrappedIdsMesh,
                finalSubtraction);

            return finalizedIdsMesh;
        }

        private void CreateFinalSupportWrapped()
        {
            if (Output.FinalSupportWrapped != null)
            {
                return;
            }

            // Create a new copy so that we dont change the original
            var itemsToWrap = Output.ReinforcementRegionIdAndExtrusionMap.Values
                .Select(extrusion => (IMesh) new IDSMesh(extrusion))
                .ToList();
            itemsToWrap.Add(Output.FinalSupport);

            WrapV2.PerformWrap(
                _teethBlockCreatorInput.Console,
                itemsToWrap.ToArray(),
                0.15,
                3,
                0.8,
                false,
                true,
                false,
                false,
                out var finalSupportWrapped);

            Output.FinalSupportWrapped = finalSupportWrapped;
        }

        private void CreateTeethBlockRoi(
            out IMesh wrappedLimitingSurfaceExtrusion,
            out IVector3D averageNormal)
        {
            averageNormal = GetLimitingSurfaceNormal();
            CreateLimitingSurfaceExtrusion(averageNormal);
            CreateTeethPart(
                out wrappedLimitingSurfaceExtrusion,
                out var teethPart);
            CreateBracketExtrusion(averageNormal);
            CreateTeethBlockRoi(teethPart);
        }

        private void CreateTeethBlockRoi(IMesh teethPart)
        {
            if (Output.TeethBlockRoi != null)
            {
                return;
            }

            var teethCast = MeshUtilitiesV2.AppendMeshes(_teethBlockCreatorInput.TeethCast.Values);

            if (!Output.BracketRegionIdAndExtrusionMap.Any())
            {
                Output.TeethBlockRoi = teethPart;
                return;
            }

            BooleansV2.PerformBooleanUnion(_teethBlockCreatorInput.Console,
                out var bracketExtrusion,
                Output.BracketRegionIdAndExtrusionMap.Values.ToArray());
            var wrappedBracketPart = GetWrappedBracketPart(
                teethCast,
                bracketExtrusion);

            Output.TeethBlockRoi = MeshUtilitiesV2.AppendMeshes(new[] { teethPart, wrappedBracketPart });
        }

        private void CreateFinalSupport(IMesh wrappedLimitingSurfaceExtrusion)
        {
            if (Output.FinalSupport != null)
            {
                return;
            }

            WrapV2.PerformWrap(
                _teethBlockCreatorInput.Console,
                new[] { Output.TeethBlockRoi },
                0.15,
                1.5,
                0.1,
                false,
                true,
                false,
                false,
                out var intermediateSupport);

            var wrappedTeethBlockBaseWithUndercut = BooleansV2.PerformBooleanSubtraction(
                _teethBlockCreatorInput.Console, 
                wrappedLimitingSurfaceExtrusion, 
                intermediateSupport);

            var limitingExtrusionAppended = MeshUtilitiesV2.AppendMeshes(
                Output.LimitingSurfaceIdAndExtrusionMap.Values);
            WrapV2.PerformWrap(
                _teethBlockCreatorInput.Console,
                new[] { limitingExtrusionAppended },
                0.3,
                0.0,
                4.9,
                false,
                true,
                false,
                false,
                out var wrappedTeethBlockBase49);

            var finalSupportBeforeFixing = BooleansV2.PerformBooleanSubtraction(
                _teethBlockCreatorInput.Console, 
                wrappedTeethBlockBase49,
                wrappedTeethBlockBaseWithUndercut);
            var finalSupport = FixFinalSupport(finalSupportBeforeFixing);

            Output.FinalSupport = finalSupport;
        }

        private IMesh FixFinalSupport(
            IMesh finalSupportToFix)
        {
            var finalSupport = AutoFixV2.RemoveNoiseShells(
                _teethBlockCreatorInput.Console, finalSupportToFix);
            finalSupport = MeshFixV2.CollapseSharpTriangles(
                _teethBlockCreatorInput.Console,
                finalSupport,
                0.001,
                30,
                3);
            finalSupport = MeshDesignV2.Stitch(
                _teethBlockCreatorInput.Console,
                finalSupport,
                0.01,
                5);
            finalSupport = AutoFixV2.PerformUnify(_teethBlockCreatorInput.Console, finalSupport);
            finalSupport = RemeshV2.PerformRemesh(
                _teethBlockCreatorInput.Console,
                finalSupport,
                0,
                1,
                0.2,
                0.01,
                0.3,
                false,
                3
            );

            return finalSupport;
        }

        private void CreateBracketExtrusion(IVector3D averageNormal)
        {
            foreach (var bracketRegion in _teethBlockCreatorInput.BracketRegions)
            {
                var bracketRegionId = bracketRegion.Key;
                var bracketRegionMesh = bracketRegion.Value;
                if (Output.BracketRegionIdAndExtrusionMap.ContainsKey(bracketRegionId))
                {
                    continue;
                }

                var bracketExtrusion = ExtrudeRegionSurface(
                    bracketRegionMesh,
                    averageNormal,
                    30,
                    true);
                Output.BracketRegionIdAndExtrusionMap.Add(bracketRegionId, bracketExtrusion);
            }
        }

        private void CreateTeethBaseExtrusion(IVector3D averageNormal)
        {
            foreach (var teethBaseRegion in _teethBlockCreatorInput.TeethBaseRegions)
            {
                var teethBaseRegionId = teethBaseRegion.Key;
                var teethBaseRegionMesh = teethBaseRegion.Value;
                if (Output.TeethBaseRegionIdAndExtrusionMap.ContainsKey(teethBaseRegionId))
                {
                    continue;
                }

                var teethBaseExtrusion = ExtrudeRegionSurface(
                    teethBaseRegionMesh,
                    averageNormal,
                    30,
                    false);
                Output.TeethBaseRegionIdAndExtrusionMap.Add(teethBaseRegionId, teethBaseExtrusion);
            }
        }

        private IMesh CreateBracketExtrusionWithDistance(IVector3D averageNormal, double extrudeDistance)
        {
            if (!_teethBlockCreatorInput.BracketRegions.Any())
            {
                return new IDSMesh();
            }

            var bracketExtrusions = _teethBlockCreatorInput.BracketRegions.Values
                .Select(bracketRegionMesh => ExtrudeRegionSurface(
                    bracketRegionMesh, 
                    averageNormal, 
                    extrudeDistance, 
                    true))
                .ToList();

            BooleansV2.PerformBooleanUnion(
                _teethBlockCreatorInput.Console,
                out var combinedBracketExtrusion,
                bracketExtrusions.ToArray());
            return combinedBracketExtrusion;
        }

        private void CreateReinforcementExtrusion(IVector3D averageNormal)
        {
            foreach (var reinforcementRegion in _teethBlockCreatorInput.ReinforcementRegions)
            {
                var reinforcementRegionId = reinforcementRegion.Key;
                var reinforcementRegionMesh = reinforcementRegion.Value;
                if (Output.ReinforcementRegionIdAndExtrusionMap.ContainsKey(reinforcementRegionId))
                {
                    continue;
                }

                var reinforcementExtrusion = ExtrudeRegionSurface(
                    reinforcementRegionMesh,
                    averageNormal,
                    30,
                    false);
                Output.ReinforcementRegionIdAndExtrusionMap.Add(
                    reinforcementRegionId, reinforcementExtrusion);
            }
        }

        private void CreateLimitingSurfaceExtrusion(IVector3D averageNormal)
        {
            foreach (var limitingSurface in _teethBlockCreatorInput.LimitingSurfaces)
            {
                var limitingSurfaceId = limitingSurface.Key;
                var limitingSurfaceMesh = limitingSurface.Value;
                if (Output.LimitingSurfaceIdAndExtrusionMap.ContainsKey(limitingSurfaceId))
                {
                    continue;
                }

                var limitingSurfaceExtrusion = ExtrudeRegionSurface(
                    limitingSurfaceMesh,
                    averageNormal,
                    30,
                    false);
                Output.LimitingSurfaceIdAndExtrusionMap.Add(limitingSurfaceId, limitingSurfaceExtrusion);
            }
        }

        private IVector3D GetLimitingSurfaceNormal()
        {
            var limitingSurface = MeshUtilitiesV2.AppendMeshes(
                _teethBlockCreatorInput.LimitingSurfaces.Values);
            var normalResult = MeshNormal.PerformNormal(
                _teethBlockCreatorInput.Console,
                limitingSurface);
            var facesNormal = normalResult.TriangleNormals;
            var averageNormal = VectorUtilitiesV2.CalculateAverageNormal(facesNormal);

            return averageNormal;
        }

        private IMesh ExtrudeRegionSurface(
            IMesh regionMesh,
            IVector3D averageNormal,
            double extrudeDistance,
            bool extrudeBothSides)
        {
            var extrudedMesh = MeshDesignV2.ExtrudeSurface(
                _teethBlockCreatorInput.Console,
                regionMesh,
                averageNormal,
                extrudeDistance);

            if (extrudeBothSides)
            {
                var extrudedMeshOpposite = MeshDesignV2.ExtrudeSurface(
                    _teethBlockCreatorInput.Console,
                    regionMesh,
                    averageNormal.Invert(),
                    extrudeDistance);

                BooleansV2.PerformBooleanUnion(
                    _teethBlockCreatorInput.Console,
                    out extrudedMesh,
                    extrudedMesh, extrudedMeshOpposite);
            }

            return extrudedMesh;
        }

        private void CreateTeethPart(
            out IMesh wrappedLimitingSurfaceExtrusion,
            out IMesh teethPart)
        {
            WrapV2.PerformWrap(
                _teethBlockCreatorInput.Console,
                Output.LimitingSurfaceIdAndExtrusionMap.Values.ToArray(),
                0.3,
                0.0,
                5,
                false,
                true,
                false,
                false,
                out wrappedLimitingSurfaceExtrusion);

            var teethCast = MeshUtilitiesV2.AppendMeshes(_teethBlockCreatorInput.TeethCast.Values);
            teethPart = BooleansV2.PerformBooleanIntersection(
                _teethBlockCreatorInput.Console,
                wrappedLimitingSurfaceExtrusion,
                teethCast);
        }

        public IMesh GetWrappedBracketPart(
            IMesh teethCast,
            IMesh bracketExtrusion)
        {
            var bracketPart = BooleansV2.PerformBooleanIntersection(
                _teethBlockCreatorInput.Console,
                teethCast,
                bracketExtrusion);
            WrapV2.PerformWrap(
                _teethBlockCreatorInput.Console,
                new[] { bracketPart },
                0.5,
                0.0,
                0.5,
                false,
                true,
                false,
                false,
                out var wrappedBracketPart);
            return wrappedBracketPart;
        }
    }
}
