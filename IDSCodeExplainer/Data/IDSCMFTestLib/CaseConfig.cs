using IDS.CMF.TestLib.Components;

namespace IDS.CMF.TestLib
{
    public class CaseConfig
    {
        public OverallInfoComponent OverallInfo { get; set; } = new OverallInfoComponent();

        public AllProPlanComponent ProPlanComponents { get; set; } = new AllProPlanComponent();

        public AllImplantCasePreferenceComponent ImplantCasePreferences { get; set; } =
            new AllImplantCasePreferenceComponent();

        public AllImplantSupportComponent ImplantSupports { get; set; } = 
            new AllImplantSupportComponent();

        public AllGuideCasePreferenceComponent GuideCasePreferences { get; set; } = 
            new AllGuideCasePreferenceComponent();

        public AllAnatomicalObstacleComponent AnatomicalObstacles { get; set; } = 
            new AllAnatomicalObstacleComponent();

        public ImplantSupportRoIInformationComponent ImplantSupportRoIInformation { get; set; } = 
            new ImplantSupportRoIInformationComponent();

        public GuideSupportRoIInformationComponent GuideSupportRoIInformation { get; set; } = 
            new GuideSupportRoIInformationComponent();

        public AllImplantDataModelComponent AllImplantDataModel { get; set; } 
            = new AllImplantDataModelComponent();

        public AllImplantScrewComponent AllImplantScrew { get; set; } = new AllImplantScrewComponent();

        public void ParseComponentsToDirector(CMFImplantDirector director, string workDir)
        {
            OverallInfo.ParseToDirector(director);
            ProPlanComponents.ParseToDirector(director, workDir);
            ImplantCasePreferences.ParseToDirector(director);
            ImplantSupports.ParseToDirector(director, workDir);
            GuideCasePreferences.ParseToDirector(director);
            AnatomicalObstacles.ParseToDirector(director, workDir);
            ImplantSupportRoIInformation.ParseToDirector(director, workDir);
            GuideSupportRoIInformation.ParseToDirector(director, workDir);
            AllImplantDataModel.ParseToDirector(director);
            AllImplantScrew.ParseToDirector(director);
        }

        public void FillToComponents(CMFImplantDirector director, string workDir)
        {
            OverallInfo.FillToComponent(director);
            ProPlanComponents.FillToComponent(director, workDir);
            ImplantCasePreferences.FillToComponent(director);
            ImplantSupports.FillToComponent(director, workDir);
            GuideCasePreferences.FillToComponent(director);
            AnatomicalObstacles.FillToComponent(director, workDir);
            ImplantSupportRoIInformation.FillToComponent(director, workDir);
            GuideSupportRoIInformation.FillToComponent(director, workDir);
            AllImplantDataModel.FillToComponent(director);
            AllImplantScrew.FillToComponent(director);
        }
    }
}
