using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.CMFImplantCreation.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;

namespace IDS.CMFImplantCreation.Creators
{
    internal class SolidMeshCreator : ComponentCreator
    {
        protected override string Name => "SolidMesh";

        public SolidMeshCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration) : base(console, componentInfo, configuration)
        {
            componentInfo.NeedToFinalize = false;
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is SolidMeshComponentInfo info))
            {
                throw new Exception("Invalid input!");
            }

            return BuildSolidMesh(info);
        }

        private Task<IComponentResult> BuildSolidMesh(SolidMeshComponentInfo info)
        {
            var timer = new Stopwatch();
            timer.Start();

            var component = new SolidMeshComponentResult
            {
                Id = _componentInfo.Id,
                IntermediateMeshes = new Dictionary<string, IMesh>(),
                IntermediateObjects = new Dictionary<string, object>(),
                ErrorMessages = new List<string>(),
                ComponentTimeTakenInSeconds = new Dictionary<string, double>()
            };

            try
            {
                var topMesh = info.TopMesh;
                var bottomMesh = info.BottomMesh;
                var offsetMesh = ImplantCreationUtilities.BuildSolidMesh(_console, info.ExtrusionMesh, ref topMesh, ref bottomMesh, out var stitched);

                component.IntermediateMeshes.Add(PastilleKeyNames.TopSolidMeshResult, topMesh);
                component.IntermediateMeshes.Add(PastilleKeyNames.BottomSolidMeshResult, bottomMesh);
                component.IntermediateMeshes.Add(PastilleKeyNames.StitchedSolidMeshResult, stitched);
                component.IntermediateMeshes.Add(PastilleKeyNames.OffsetSolidMeshResult, offsetMesh);
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
