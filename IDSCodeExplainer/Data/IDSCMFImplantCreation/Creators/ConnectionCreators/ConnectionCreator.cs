using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.CMFImplantCreation.Utilities;
using IDS.Core.V2.Extensions;
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
    internal class ConnectionCreator : ComponentCreator
    {
        protected override string Name => "Connection";

        public ConnectionCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
            : base(console, componentInfo, configuration)
        {

        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is ConnectionComponentInfo info))
            {
                throw new Exception("Invalid input!");
            }

            return CreateConnection(info);
        }

        private Task<IComponentResult> CreateConnection(ConnectionComponentInfo info)
        {
            var timer = new Stopwatch();
            timer.Start();

            var component = new ConnectionComponentResult
            {
                Id = _componentInfo.Id,
                IntermediateMeshes = new Dictionary<string, IMesh>(),
                IntermediateObjects = new Dictionary<string, object>(),
                ErrorMessages = new List<string>()
            };

            try
            {
                GenerateConnection(info, ref component);
            }
            catch (Exception e)
            {
                component.ErrorMessages.Add(e.Message);
            }

            timer.Stop();
            component.TimeTakenInSeconds = timer.ElapsedMilliseconds * 0.001;

            return Task.FromResult(component as IComponentResult);
        }

        private void GenerateConnection(ConnectionComponentInfo info, ref ConnectionComponentResult component)
        {
            var intersectionCurve = GetConnectionIntersectionCurve(
                info, info.ConnectionCurve, ref component);

            var pulledCurve = component.IntermediateObjects.GetLast(
                ConnectionKeyNames.PulledCurveResult) as ICurve;
            GetConnectionMesh(info, intersectionCurve, pulledCurve,
                false,
                ref component, 
                out var sharpCurves, 
                out var connectionMesh);
            var individualImplantParams = _configuration
                .GetIndividualImplantParameter();

            var tubeRadius =
                ImplantWrapAndOffsetPredictor.GetTubeRadius(
                    individualImplantParams, info.Thickness, info.Width);
            foreach (var sharpCurve in sharpCurves)
            {
                var sharpIntersectionCurve = GetConnectionIntersectionCurve(
                    info, sharpCurve, ref component);

                var sharpPulledCurve = component.IntermediateObjects.GetLast(ConnectionKeyNames.PulledCurveResult) as ICurve;
                if (sharpPulledCurve.GetLength() < tubeRadius)
                {
                    continue;
                }

                GetConnectionMesh(info, sharpIntersectionCurve, sharpPulledCurve,
                    true,
                    ref component,
                    out _, out var sharpConnectionMesh);
                //atm, only one sharp angle curve
                component.IntermediateMeshes.Add(ConnectionKeyNames.SharpConnectionMeshResult, sharpConnectionMesh);
                connectionMesh = MeshUtilitiesV2.AppendMeshes(
                    new[] { sharpConnectionMesh, connectionMesh });
            }

            component.ComponentMesh = connectionMesh;
        }

        private void TransferAllGeneralResults(IComponentResult componentResult, 
            ref ConnectionComponentResult connectionComponentResult)
        {
            connectionComponentResult.ComponentMesh = componentResult.ComponentMesh;
            connectionComponentResult.FinalComponentMesh = componentResult.FinalComponentMesh;

            foreach (var keyValuePair in componentResult.IntermediateMeshes)
            {
                connectionComponentResult.IntermediateMeshes.Append(keyValuePair.Key, keyValuePair.Value);
            }

            foreach (var keyValuePair in componentResult.IntermediateObjects)
            {
                connectionComponentResult.IntermediateObjects.Append(keyValuePair.Key, keyValuePair.Value);
            }
        }

        private ICurve GetConnectionIntersectionCurve(
            ConnectionComponentInfo info, ICurve connectionCurve,
            ref ConnectionComponentResult component)
        {
            var componentFactory = new ComponentFactory();
            var intersectionCurveComponentInfo = info.
                ToActualComponentInfo<ConnectionIntersectionCurveComponentInfo>();
            intersectionCurveComponentInfo.ConnectionCurve = connectionCurve;

            var intersectionCurveCreator = componentFactory.CreateComponentCreator(
                _console, intersectionCurveComponentInfo, _configuration);
            var intersectionCurveComponentResult = 
                intersectionCurveCreator.CreateComponentAsync().Result
                    as ConnectionIntersectionCurveComponentResult;

            TransferAllGeneralResults(intersectionCurveComponentResult, ref component);

            if (intersectionCurveComponentResult.ErrorMessages.Any())
            {
                throw new Exception(intersectionCurveComponentResult.ErrorMessages.Last());
            }

            var intersectionCurve = component.IntermediateObjects.GetLast(
                ConnectionKeyNames.IntersectionCurveResult) as ICurve;

            return intersectionCurve;
        }

        private void GetConnectionMesh(
            ConnectionComponentInfo info, 
            ICurve intersectionCurve,
            ICurve pulledCurve,
            bool isSharpConnection,
            ref ConnectionComponentResult component,
            out List<ICurve> sharpCurves,
            out IMesh connectionMesh)
        {
            var individualImplantParams = _configuration
                .GetIndividualImplantParameter();
            var componentFactory = new ComponentFactory();

            var generateConnectionComponentInfo = info.ToActualComponentInfo<GenerateConnectionComponentInfo>();
            var wrapBasis = ImplantWrapAndOffsetPredictor
                .GetBasis(individualImplantParams, info.Thickness, info.Width);
            generateConnectionComponentInfo.WrapBasis = wrapBasis;
            generateConnectionComponentInfo.IsSharpConnection = isSharpConnection;
            generateConnectionComponentInfo.IntersectionCurve = intersectionCurve;
            generateConnectionComponentInfo.PulledCurve = pulledCurve;

            var generateConnectionCreator = componentFactory.CreateComponentCreator(_console, generateConnectionComponentInfo, _configuration);
            var generateConnectionComponentResult = generateConnectionCreator.CreateComponentAsync().Result as GenerateConnectionComponentResult;

            TransferAllGeneralResults(generateConnectionComponentResult, ref component);

            if (generateConnectionComponentResult.ErrorMessages.Any())
            {
                throw new Exception(generateConnectionComponentResult.ErrorMessages.Last());
            }

            sharpCurves = component.IntermediateObjects.GetLast(
                ConnectionKeyNames.SharpCurvesResult) as List<ICurve>;
            connectionMesh = component.IntermediateMeshes.GetLast(
                ConnectionKeyNames.ConnectionMeshResult);
        }
    }
}
