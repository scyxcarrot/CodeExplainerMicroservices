using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
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
    internal class FinalizationCreator : ComponentCreator
    {
        protected override string Name => "Finalization";

        public FinalizationCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
            : base(console, componentInfo, configuration)
        {
            isSingleShell = true;
            componentInfo.NeedToFinalize = true;
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is FinalizationComponentInfo info))
            {
                throw new Exception("Invalid input!");
            }

            var componentResult = new FinalizationComponentResult
            {
                Id = _componentInfo.Id,
                IntermediateMeshes = new Dictionary<string, IMesh>(),
                IntermediateObjects = new Dictionary<string, object>(),
                ErrorMessages = new List<string>(),
                ComponentTimeTakenInSeconds = new Dictionary<string, double>()
            };

            if (info.ComponentMeshes == null || !info.ComponentMeshes.Any())
            {
                componentResult.ErrorMessages.Add("ComponentMeshes is Empty!");
            }
            else
            {
                var timer = new Stopwatch();
                timer.Start();

                var appended = MeshUtilitiesV2.AppendMeshes(info.ComponentMeshes);

                var overallImplantParams = _configuration.GetOverallImplantParameter();
                var componentSmallestDetail = overallImplantParams.WrapOperationSmallestDetails;
                if (!WrapV2.PerformWrap(_console, new[] { appended }, componentSmallestDetail, 2, 0, false, false, false, false, out var wrapped))
                {
                    componentResult.ErrorMessages.Add("wrapped implant plate failed.");
                }

                componentResult.ComponentMesh = wrapped;

                timer.Stop();

                componentResult.TimeTakenInSeconds = timer.ElapsedMilliseconds * 0.001;
                componentResult.ComponentTimeTakenInSeconds.Add(Name, componentResult.TimeTakenInSeconds);
            }

            return Task.FromResult(componentResult as IComponentResult);
        }
    }
}
