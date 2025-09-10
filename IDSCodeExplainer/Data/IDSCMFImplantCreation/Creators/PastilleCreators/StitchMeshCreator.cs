using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.CMFImplantCreation.Utilities;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IDS.CMFImplantCreation.Creators
{
    internal class StitchMeshCreator : ComponentCreator
    {
        protected override string Name => "StitchMesh";

        public StitchMeshCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration) : base(console, componentInfo, configuration)
        {
            componentInfo.NeedToFinalize = false;
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            var info = _componentInfo as StitchMeshComponentInfo;

            if (info == null)
            {
                throw new Exception("Invalid input!");
            }

            return CreateStitchMesh(info);
        }

        private Task<IComponentResult> CreateStitchMesh(StitchMeshComponentInfo info)
        {
            var timer = new Stopwatch();
            timer.Start();

            var component = new StitchMeshComponentResult()
            {
                Id = _componentInfo.Id,
                IntermediateMeshes = new Dictionary<string, IMesh>(),
                IntermediateObjects = new Dictionary<string, object>(),
                ErrorMessages = new List<string>(),
                ComponentTimeTakenInSeconds = new Dictionary<string, double>()
            };

            try
            {
                var stitched = MeshUtilities.StitchMeshSurfaces(
                    _console, info.TopMesh, info.BottomMesh);
                var combinedMesh =
                    MeshUtilitiesV2.AppendMeshes(new IMesh[] { stitched, info.TopMesh, info.BottomMesh });
                var offset = AutoFixV2.PerformBasicAutoFix(_console, combinedMesh,3);

                component.IntermediateMeshes.Add(PastilleKeyNames.StitchedStitchMeshResult, stitched);
                component.IntermediateMeshes.Add(PastilleKeyNames.OffsetStitchMeshResult, offset);
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
