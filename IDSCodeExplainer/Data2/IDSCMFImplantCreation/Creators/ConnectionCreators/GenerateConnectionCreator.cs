using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.CMFImplantCreation.Utilities;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace IDS.CMFImplantCreation.Creators
{
    internal class GenerateConnectionCreator : ComponentCreator
    {
        protected override string Name => "GenerateConnection";

        public GenerateConnectionCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
            : base(console, componentInfo, configuration)
        {
            componentInfo.NeedToFinalize = false;
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is GenerateConnectionComponentInfo info))
            {
                throw new Exception("Invalid input!");
            }

            return GetConnectionMeshAndSharpCurves(info);
        }

        private Task<IComponentResult> GetConnectionMeshAndSharpCurves(
            GenerateConnectionComponentInfo info)
        {
            var timer = new Stopwatch();
            timer.Start();

            var component = new GenerateConnectionComponentResult()
            {
                Id = _componentInfo.Id,
                IntermediateMeshes = new Dictionary<string, IMesh>(),
                IntermediateObjects = new Dictionary<string, object>(),
                ErrorMessages = new List<string>()
            };

            try
            {
                var individualImplantParams = _configuration.GetIndividualImplantParameter();
                var connectionThickness = info.Thickness;
                var connectionWidth = info.Width;
                var wrapBasis = info.WrapBasis;
                var isSharpConnection = info.IsSharpConnection;

                var lowerOffsetCompensation = 
                    ImplantWrapAndOffsetPredictor
                        .CalculateLowerOffsetCompensation(
                            individualImplantParams,
                            connectionThickness,
                            connectionWidth,
                            wrapBasis);
                var finalWrapOffset = wrapBasis * individualImplantParams.WrapOperationOffsetInDistanceRatio;
                var offsetDistanceLower = (connectionThickness - finalWrapOffset) / 2 - lowerOffsetCompensation;
                var offsetDistanceUpper = connectionThickness - finalWrapOffset;

                if (offsetDistanceUpper < 0.00)
                {
                    throw new Exception(
                        "Implant pastille thickness and width ratio invalid.");
                }

                var connectionSurface = ImplantCreationUtilities
                    .GetPatch(
                    _console, info.SupportRoIMesh, info.IntersectionCurve, false);
                connectionSurface = AutoFixV2.RemoveFreePoints(
                    _console, connectionSurface);

                if (info.IsActual)
                {
                    connectionSurface = RemeshV2.PerformRemesh(_console, connectionSurface, 0.0, 0.2, 0.2, 0.01, 0.3, false, 3);
                }
                else
                {
                    connectionSurface = RemeshV2.PerformRemesh(_console, connectionSurface, 0.0, 0.4, 0.2, 0.01, 0.3, false, 3);
                }

                var connectionPoints =
                    Curves.GetEquidistantPointsOnCurve(_console, 
                    info.PulledCurve, 0.05);

                var connectionVertexAndNormals =
                    VectorUtilities.GetConnectionPointsWithNormals(
                        _console,
                        connectionPoints, info.SupportRoIMesh);
                connectionPoints.Clear();

                var sizeOnBothEnds = isSharpConnection ? 1 : 20;
                var interpolatedConnectionVertexAndNormals = 
                    VectorUtilities.InterpolateNormal(
                        connectionVertexAndNormals, sizeOnBothEnds, 
                        out var startDiviate, out var endDiviate);
                var connectionSurfaceVertexAndNormals =
                    VectorUtilities.GetConnectionSurfaceVertexAndNormals(connectionSurface, interpolatedConnectionVertexAndNormals);

                if (isSharpConnection)
                {
                    connectionSurfaceVertexAndNormals =
                        VectorUtilities.UniformizeVertexAndNormals(
                            connectionSurfaceVertexAndNormals);
                }
                else
                {
                    var vertexAndNormalToReplace =
                        VectorUtilities.FixAbnormalsNormals(
                            _console, connectionSurfaceVertexAndNormals, connectionSurface,
                            connectionThickness + 0.1, 70);

                    vertexAndNormalToReplace.ForEach(x =>
                    {
                        connectionSurfaceVertexAndNormals[x.Key] = x.Value;
                    });

                    VectorUtilities.UpdateByClosestAround(
                        ref connectionSurfaceVertexAndNormals,
                        vertexAndNormalToReplace);

                    VectorUtilities.UpdateByClosestAroundAndClosestWithTheNormal(
                        ref connectionSurfaceVertexAndNormals,
                        vertexAndNormalToReplace,
                        connectionVertexAndNormals);
                    var sharpCurves = CurveUtilities.GetSharpAngleCurves(
                        interpolatedConnectionVertexAndNormals
                            .Select(i => i.Point)
                            .ToList(), startDiviate, endDiviate);
                    component.IntermediateObjects.Add(ConnectionKeyNames.SharpCurvesResult, sharpCurves);
                }

                VectorUtilities.GetOffsetVerticesLowerAndUpper(
                    _console,
                    connectionSurfaceVertexAndNormals, 
                    connectionSurface,
                    offsetDistanceLower, offsetDistanceUpper, 
                    isSharpConnection,
                    out var offsetVerticesLower, 
                    out var offsetVerticesUpper);
                connectionSurfaceVertexAndNormals.Clear();

                var smallestDetail = individualImplantParams.WrapOperationSmallestDetails;
                var gapClosingDistance = individualImplantParams.WrapOperationGapClosingDistance;
                var wrappedMesh = OptimizeOffsetUtilities.OptimizeOffsetAndWrap(
                    _console,
                    offsetVerticesLower,
                    offsetVerticesUpper,
                    connectionSurface, 
                    smallestDetail, gapClosingDistance, finalWrapOffset);
                component.IntermediateMeshes.Add(
                    ConnectionKeyNames.ConnectionMeshResult, wrappedMesh);
            }
            catch (Exception e)
            {
                component.ErrorMessages.Add(e.Message);
            }

            timer.Stop();
            component.TimeTakenInSeconds = timer.ElapsedMilliseconds * 0.001;

            return Task.FromResult(component as IComponentResult);
        }
    }
}
