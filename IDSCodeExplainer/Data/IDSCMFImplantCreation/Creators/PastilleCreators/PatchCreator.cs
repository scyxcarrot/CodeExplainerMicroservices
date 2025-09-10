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
    internal class PatchCreator : ComponentCreator
    {
        protected override string Name => "Patch";

        public PatchCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
            : base(console, componentInfo, configuration)
        {
            //temporary disable finalization
            componentInfo.NeedToFinalize = false;
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is PatchComponentInfo info))
            {
                throw new Exception("Invalid input!");
            }

            return GetPatch(info);
        }

        private Task<IComponentResult> GetPatch(PatchComponentInfo info)
        {
            var timer = new Stopwatch();
            timer.Start();

            var component = new PatchComponentResult
            {
                Id = _componentInfo.Id,
                IntermediateMeshes = new Dictionary<string, IMesh>(),
                IntermediateObjects = new Dictionary<string, object>(),
                ErrorMessages = new List<string>(),
                ComponentTimeTakenInSeconds = new Dictionary<string, double>()
            };

            try
            {
                var patch = ImplantCreationUtilities.GetPatch(
                    _console, info.SupportRoIMesh, info.IntersectionCurve, false);

                var surface = TrianglesV2.PerformRemoveOverlappingTriangles(_console, patch);
                surface = AutoFixV2.PerformBasicAutoFix(_console, surface, 1);

                component.IntermediateMeshes.Add(PastilleKeyNames.ConnectionSurfaceResult, surface);

                //continue with offset
                OptimizeOffsetUtilities.CreatePastilleOptimizeOffset(_console, info.DoUniformOffset,
                   info.Location, info.SupportRoIMesh, surface,
                   info.OffsetDistanceUpper, info.OffsetDistance,
                   out var pointsUpper, out var pointsLower,
                   out var top, out var bottom);

                component.IntermediateMeshes.Add(PastilleKeyNames.OffsetTopResult, top);
                component.IntermediateMeshes.Add(PastilleKeyNames.OffsetBottomResult, bottom);
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
