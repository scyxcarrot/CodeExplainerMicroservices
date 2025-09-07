using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.TestLib.Components
{
    public class AllImplantDataModelComponent
    {
        public List<ImplantDataModelComponent> ImplantDataModels { get; set; } 
            = new List<ImplantDataModelComponent>();

        public void ParseToDirector(CMFImplantDirector director)
        {
            foreach (var implantDataModel in ImplantDataModels)
            {
                var caseGuid = implantDataModel.CaseGuid;
                var casePreferenceDataModel = director.CasePrefManager.CasePreferences.First(c => c.CaseGuid == caseGuid);
                casePreferenceDataModel.ImplantDataModel = implantDataModel.GetImplantDataModel();

                director.ImplantManager.AddAllConnectionsBuildingBlock(casePreferenceDataModel);
                director.ImplantManager.AddPlanningImplantBuildingBlock(casePreferenceDataModel);
                director.ImplantManager.AddLandmarksBuildingBlock(casePreferenceDataModel);
            }
        }

        public void FillToComponent(CMFImplantDirector director)
        {
            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var implantDataModel = new ImplantDataModelComponent();
                implantDataModel.SetImplantDataModel(casePreferenceDataModel.CaseGuid,
                    casePreferenceDataModel.ImplantDataModel);
                ImplantDataModels.Add(implantDataModel);
            }
        }
    }
}
