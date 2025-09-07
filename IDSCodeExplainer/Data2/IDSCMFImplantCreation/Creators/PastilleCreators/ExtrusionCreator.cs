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
    internal class ExtrusionCreator : ComponentCreator
    {
        protected override string Name => "Extrusion";

        public ExtrusionCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
            : base(console, componentInfo, configuration)
        {
            componentInfo.NeedToFinalize = false;
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is ExtrusionComponentInfo info))
            {
                throw new Exception("Invalid input!");
            }

            return GetExtrusion(info);
        }

        private Task<IComponentResult> GetExtrusion(ExtrusionComponentInfo info)
        {
            var timer = new Stopwatch();
            timer.Start();

            var component = new ExtrusionComponentResult
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

                var extrudeIntersectionCurve = ImplantCreationUtilities.GetIntersectionCurveForPastille(_console, info.ExtrudeCylinder, info.Location, info.SupportRoIMesh, info.Direction);
                var extrusion = Curves.ExtrudeCurve(_console, extrudeIntersectionCurve, info.Direction, info.Thickness);

                component.IntermediateObjects.Add(PastilleKeyNames.ExtrudeIntersectionCurveResult, extrudeIntersectionCurve);
                component.IntermediateMeshes.Add(PastilleKeyNames.ExtrusionResult, extrusion);
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
    }
}
