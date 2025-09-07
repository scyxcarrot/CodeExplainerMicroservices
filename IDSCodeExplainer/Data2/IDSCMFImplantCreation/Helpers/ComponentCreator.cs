using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Utilities;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Tools;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace IDS.CMFImplantCreation.Helpers
{
    internal abstract class ComponentCreator : IComponentCreator
    {
        protected readonly IConsole _console;
        protected readonly IComponentInfo _componentInfo;
        protected readonly IConfiguration _configuration;

        protected virtual string Name => "BaseComponent";

        protected bool isSingleShell;

        public ComponentCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
        {
            _console = console;
            _componentInfo = componentInfo;
            _configuration = configuration;
        }

        public Task<IComponentResult> CreateComponentAsync()
        {
            if (_componentInfo.Id == Guid.Empty)
            {
                _componentInfo.Id = Guid.NewGuid();
            }

            _console.WriteDiagnosticLine($"Start Create {Name}: {_componentInfo.Id}");

            var component = CreateSubComponentAsync();

            _console.WriteDiagnosticLine($"Done Create {Name}: {_componentInfo.Id}");

            return component;
        }

        public Task<IComponentResult> FinalizeComponentAsync(IComponentResult component)
        {
            return Task.FromResult(FinalizeComponent(component));
        }

        protected abstract Task<IComponentResult> CreateSubComponentAsync();

        public IComponentResult FinalizeComponent(IComponentResult component)
        {
            var timer = new Stopwatch();
            timer.Start();

            var mesh = component.ComponentMesh;

            if (_componentInfo.NeedToFinalize)
            {
                try
                {
                    var overallImplantParams = _configuration.GetOverallImplantParameter();

                    var wrapRatio = overallImplantParams.WrapOperationOffset;
                    var componentSmallestDetail = overallImplantParams.WrapOperationSmallestDetails;
                    var componentGapClosingDistance = overallImplantParams.WrapOperationGapClosingDistance;
                    if (!WrapV2.PerformWrap(_console, new[] { mesh }, componentSmallestDetail, componentGapClosingDistance, wrapRatio, false, true, false, false, out var wrapped))
                    {
                        throw new Exception("wrapped implant plate failed.");
                    }

                    var componentMesh = wrapped as IDSMesh;

                    if (_componentInfo.IsActual)
                    {
                        componentMesh = ImplantCreationUtilities.RemeshAndSmoothImplant(_console, componentMesh) as IDSMesh;
                    }

                    var componentWithoutSubtraction = BooleansV2.PerformBooleanSubtraction(_console, componentMesh, _componentInfo.ClearanceMesh) as IDSMesh;
                    component.IntermediateMeshes.Add("ComponentWithoutSubtraction", componentWithoutSubtraction);

                    //for pastille only, if contains disjoint shells, need to filter out 

                    if (_componentInfo.Subtractors.Any())
                    {
                        var substractorMesh = new IDSMesh();

                        foreach (var subtractor in _componentInfo.Subtractors)
                        {
                            substractorMesh.Append(subtractor);
                        }

                        componentMesh = BooleansV2.PerformBooleanSubtraction(_console, componentWithoutSubtraction, substractorMesh) as IDSMesh;
                    }

                    if (isSingleShell)
                    {
                        var meshDiagnostics = MeshDiagnostics.GetMeshDiagnostics(_console, componentMesh);
                        if (meshDiagnostics.NumberOfShells > 1)
                        {
                            componentMesh = ImplantCreationUtilities.GetLargestSurfaceAreaShell(_console, componentMesh) as IDSMesh;
                        }
                    }

                    if (_componentInfo.IsActual)
                    {
                        var fixingTimer = new Stopwatch();
                        fixingTimer.Start();

                        var resultantMesh = MeshFixingUtilities.PerformComplexFullyFix(_console, componentMesh,
                            overallImplantParams.FixingIterations, 0.0010, 30.000);

                        componentMesh = resultantMesh as IDSMesh;

                        fixingTimer.Stop();
                        component.FixingTimeInSeconds = fixingTimer.ElapsedMilliseconds * 0.001;
                    }
                    else
                    {
                        componentMesh = AutoFixV2.RemoveNoiseShells(_console, componentMesh) as IDSMesh;
                    }

                    component.FinalComponentMesh = componentMesh;
                }
                catch (Exception e)
                {
                    _console.WriteErrorLine($"Exception thrown {Name}: {_componentInfo.Id} - {e.Message}");
                    component.ErrorMessages.Add(e.Message);
                }
            }
            else
            {
                component.FinalComponentMesh = mesh;
            }

            timer.Stop();
            component.TimeTakenInSeconds += timer.ElapsedMilliseconds * 0.001;

            return component;
        }
    }
}
