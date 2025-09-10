using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.TestLib.Components
{
    public class AllProPlanComponent
    {
        public MedicalCoordinateSystemComponent MedicalCoordinateSystem { get; set; } =
            new MedicalCoordinateSystemComponent();
        public List<ProPlanComponent> PreopComponents { get; set; } =
            new List<ProPlanComponent>();

        public List<ProPlanComponent> OriginalComponents { get; set; } =
            new List<ProPlanComponent>();

        public List<ProPlanComponent> PlannedComponents { get; set; } =
            new List<ProPlanComponent>();

        public void ParseToDirector(CMFImplantDirector director, string workDir)
        {
            MedicalCoordinateSystem.ParseToDirector(director);

            foreach (var component in PreopComponents)
            {
                component.ParseToDirector(director, workDir);
            }

            foreach (var component in OriginalComponents)
            {
                component.ParseToDirector(director, workDir);
            }

            foreach (var component in PlannedComponents)
            {
                component.ParseToDirector(director, workDir);
            }

            if (PreopComponents.Any() &&
                OriginalComponents.Any() &&
                PlannedComponents.Any())
            {
                ProPlanImportUtilities.PostProPlanPartsCreation(director, out _);
            }
        }

        public void FillToComponent(CMFImplantDirector director, string workDir)
        {
            MedicalCoordinateSystem.FillToComponent(director);

            var preopObjects = ProPlanImportUtilities.GetAllProPlanObjects(director.Document, ProplanBoneType.Preop);
            foreach (var preopObject in preopObjects)
            {
                var proPlanComponent = new ProPlanComponent();
                proPlanComponent.FillToComponent(preopObject, workDir);
                PreopComponents.Add(proPlanComponent);
            }

            var originalObjects = ProPlanImportUtilities.GetAllProPlanObjects(director.Document, ProplanBoneType.Original);
            foreach (var originalObject in originalObjects)
            {
                var proPlanComponent = new ProPlanComponent();
                proPlanComponent.FillToComponent(originalObject, workDir);
                OriginalComponents.Add(proPlanComponent);
            }

            var plannedObjects = ProPlanImportUtilities.GetAllProPlanObjects(director.Document, ProplanBoneType.Planned);
            foreach (var plannedObject in plannedObjects)
            {
                var proPlanComponent = new ProPlanComponent();
                proPlanComponent.FillToComponent(plannedObject, workDir);
                PlannedComponents.Add(proPlanComponent);
            }
        }
    }
}
