using IDS.CMF.CasePreferences;
using System;

namespace IDS.CMF.TestLib.Components
{
    public class ImplantCasePreferenceComponent
    {
        public string ImplantType { get; set; }

        public string ScrewType { get; set; }

        public string BarrelType { get; set; }

        public int CaseNum { get; set; } = -1;

        public Guid CaseGuid { get; set; } = Guid.NewGuid();

        public double PlateThicknessMm { get; set; }

        public double PlateWidthMm { get; set; }

        public double LinkWidthMm { get; set; }

        public string ScrewStyle { get; set; }

        private ImplantPreferenceModel AddNewImplantCase(CMFImplantDirector director)
        {
            var dataModel = new ImplantPreferenceModel(director.CasePrefManager.SurgeryInformation.SurgeryType,
                director.ScrewBrandCasePreferences, director.ScrewLengthsPreferences, CaseGuid);
            director.CasePrefManager.AddCasePreference(dataModel);
            return dataModel;
        }

        public void ParseToDirector(CMFImplantDirector director)
        {
            var implantPreferenceModel = AddNewImplantCase(director);
            implantPreferenceModel.SelectedImplantType = ImplantType;
            implantPreferenceModel.SelectedScrewType = ScrewType;
            implantPreferenceModel.SelectedScrewStyle = ScrewStyle;
            implantPreferenceModel.SelectedBarrelType = BarrelType;
            implantPreferenceModel.CasePrefData.PlateThicknessMm = PlateThicknessMm;
            implantPreferenceModel.CasePrefData.PlateWidthMm = PlateWidthMm;
            implantPreferenceModel.CasePrefData.LinkWidthMm = LinkWidthMm;
            implantPreferenceModel.SetCaseNumber(CaseNum);
            implantPreferenceModel.UpdateScrewAide();
        }

        public void FillToComponent(CasePreferenceDataModel casePreferenceDataModel)
        {
            ImplantType = casePreferenceDataModel.CasePrefData.ImplantTypeValue;
            ScrewType = casePreferenceDataModel.CasePrefData.ScrewTypeValue;
            BarrelType = casePreferenceDataModel.CasePrefData.BarrelTypeValue;
            CaseNum = casePreferenceDataModel.NCase;
            CaseGuid = casePreferenceDataModel.CaseGuid;
            PlateThicknessMm = casePreferenceDataModel.CasePrefData.PlateThicknessMm;
            PlateWidthMm = casePreferenceDataModel.CasePrefData.PlateWidthMm;
            LinkWidthMm = casePreferenceDataModel.CasePrefData.LinkWidthMm;
            ScrewStyle = casePreferenceDataModel.CasePrefData.ScrewStyle;
        }
    }
}
