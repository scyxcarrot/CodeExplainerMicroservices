using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.CMFImplantCreation.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IDS.CMFImplantCreation.Creators
{
    internal class PastilleIntersectionCurveCreator : ComponentCreator
    {
        protected override string Name => "PastilleIIntersectionCurve";

        public PastilleIntersectionCurveCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
            : base(console, componentInfo, configuration)
        {
            componentInfo.NeedToFinalize = false;
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is PastilleIntersectionCurveComponentInfo info))
            {
                throw new Exception("Invalid input!");
            }

            return GetIntersectionCurve(info);
        }

        private Task<IComponentResult> GetIntersectionCurve(PastilleIntersectionCurveComponentInfo info)
        {
            var timer = new Stopwatch();
            timer.Start();

            var component = new PastilleIntersectionCurveComponentResult
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

                var individualImplantParams = _configuration.GetIndividualImplantParameter();

                IMesh intersectionMesh = null;

                if (info.CreationAlgoMethod == PastilleKeyNames.CreationAlgoPrimaryMethod)
                {
                    var pastilleCylinder = ImplantCreationUtilities.GeneratePastilleCylinderIntersectionMesh(_console, individualImplantParams, model);
                    var pastilleExtrudeCylinder = ImplantCreationUtilities.GeneratePastilleExtrudeCylinderIntersectionMesh(_console, individualImplantParams, model);
                    intersectionMesh = pastilleCylinder;

                    component.IntermediateMeshes.Add(PastilleKeyNames.CylinderResult, pastilleCylinder);
                    component.IntermediateMeshes.Add(PastilleKeyNames.CylinderExtrudeResult, pastilleExtrudeCylinder);
                    component.CreationAlgoMethod = PastilleKeyNames.CreationAlgoPrimaryMethod;
                }
                else //PastilleKeyNames.CreationAlgoSecondaryMethod
                {
                    var pastilleSphere = ImplantCreationUtilities.GeneratePastilleSphereIntersectionMesh(_console, individualImplantParams, model);
                    var pastilleExtrudeSphere = ImplantCreationUtilities.GeneratePastilleExtrudeSphereIntersectionMesh(_console, individualImplantParams, model);
                    intersectionMesh = pastilleExtrudeSphere;

                    component.IntermediateMeshes.Add(PastilleKeyNames.SphereResult, pastilleSphere);
                    component.IntermediateMeshes.Add(PastilleKeyNames.SphereExtrudeResult, pastilleExtrudeSphere);
                    component.CreationAlgoMethod = PastilleKeyNames.CreationAlgoSecondaryMethod;
                }

                var intersectionCurve = ImplantCreationUtilities.GetIntersectionCurveForPastille(_console, intersectionMesh, info.Location, info.SupportRoIMesh, info.Direction);

                component.IntermediateObjects.Add(PastilleKeyNames.IntersectionCurveResult, intersectionCurve);
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
