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
using System.Threading.Tasks;

namespace IDS.CMFImplantCreation.Creators
{
    internal class ConnectionIntersectionCurveCreator : ComponentCreator
    {
        protected override string Name => "ConnectionIntersectionCurve";

        public ConnectionIntersectionCurveCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
            : base(console, componentInfo, configuration)
        {
            componentInfo.NeedToFinalize = false;
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is ConnectionIntersectionCurveComponentInfo info))
            {
                throw new Exception("Invalid input!");
            }

            return GetIntersectionCurve(info);
        }

        private Task<IComponentResult> GetIntersectionCurve(ConnectionIntersectionCurveComponentInfo info)
        {
            var timer = new Stopwatch();
            timer.Start();

            var component = new ConnectionIntersectionCurveComponentResult
            {
                Id = _componentInfo.Id,
                IntermediateMeshes = new Dictionary<string, IMesh>(),
                IntermediateObjects = new Dictionary<string, object>(),
                ErrorMessages = new List<string>()
            };

            try
            {
                var individualImplantParams = _configuration.GetIndividualImplantParameter();
                var tubeRadius = ImplantWrapAndOffsetPredictor.GetTubeRadius(individualImplantParams, info.Thickness, info.Width);

                var pulledCurve = Curves.AttractCurve(_console, info.SupportMeshFull, info.ConnectionCurve);

                var tube = Lattice.CreateMeshFromCurve(_console, pulledCurve, tubeRadius);

                var intersectionCurve = ImplantCreationUtilities.GetIntersectionCurveForConnection(_console, tube, info.SupportRoIMesh, info.AverageConnectionDirection);

                component.IntermediateObjects.Add(ConnectionKeyNames.PulledCurveResult, pulledCurve);
                component.IntermediateMeshes.Add(ConnectionKeyNames.TubeResult, tube);
                component.IntermediateObjects.Add(ConnectionKeyNames.IntersectionCurveResult, intersectionCurve);
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
