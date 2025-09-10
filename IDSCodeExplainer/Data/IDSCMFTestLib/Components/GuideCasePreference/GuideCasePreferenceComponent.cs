using IDS.CMF.CasePreferences;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.TestLib.Components
{
    public class GuideCasePreferenceComponent
    {
        public string GuideType { get; set; }

        public string ScrewType { get; set; }

        public int CaseNum { get; set; } = -1;

        public Guid CaseGuid { get; set; } = Guid.NewGuid();

        public List<Guid> LinkedImplantScrews { get; set; } = new List<Guid>();

        private GuidePreferenceModel AddNewGuideCase(CMFImplantDirector director)
        {
            var dataModel = new GuidePreferenceModel(director.ScrewBrandCasePreferences, CaseGuid);
            director.CasePrefManager.AddGuidePreference(dataModel);
            return dataModel;
        }

        public void ParseToDirector(CMFImplantDirector director)
        {
            var guidePreferenceDataModel = AddNewGuideCase(director);
            guidePreferenceDataModel.SelectedGuideType = GuideType;
            guidePreferenceDataModel.SelectedGuideScrewType = ScrewType;
            guidePreferenceDataModel.SetCaseNumber(CaseNum);
            guidePreferenceDataModel.UpdateGuideFixationScrewAide();
            guidePreferenceDataModel.LinkedImplantScrews.AddRange(LinkedImplantScrews);
        }

        public void FillToComponent(GuidePreferenceDataModel guidePreferenceDataModel)
        {
            GuideType = guidePreferenceDataModel.GuidePrefData.GuideTypeValue;
            ScrewType = guidePreferenceDataModel.GuidePrefData.GuideScrewTypeValue;
            CaseNum = guidePreferenceDataModel.NCase;
            LinkedImplantScrews = guidePreferenceDataModel.LinkedImplantScrews.ToList();
        }
    }
}
