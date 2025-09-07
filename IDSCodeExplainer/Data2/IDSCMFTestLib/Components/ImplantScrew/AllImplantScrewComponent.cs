using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.TestLib.Components
{
    public class AllImplantScrewComponent
    {
        public bool NeedRecalibration { get; set; } = true;
        public bool NeedBarrelRegistration { get; set; } = true;
        public int ScrewGroupCount { get; set; } = 0;

        public Dictionary<Guid, List<ImplantScrewComponent>> Screws { get; set; } =
            new Dictionary<Guid, List<ImplantScrewComponent>>();

        private void RecalibrationScrewForTheCase(CMFImplantDirector director, CasePreferenceDataModel casePreferenceDataModel)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var implantSupportBb =  implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, casePreferenceDataModel);

            var objectManager = new CMFObjectManager(director);

            if (objectManager.HasBuildingBlock(implantSupportBb))
            {
                var supportMesh = objectManager.GetBuildingBlock(implantSupportBb);
                var roiVolume = ImplantCreationUtilities.GetImplantRoIVolume(objectManager, casePreferenceDataModel, ref supportMesh);
                supportMesh.Dispose();

                var scrManager = new ScrewManager(director);
                if (!scrManager.CalibrateAllImplantScrew(roiVolume, casePreferenceDataModel))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Failed to calibrate screws for case {casePreferenceDataModel.CaseName}");
                }
            }
        }

        public void ParseToDirector(CMFImplantDirector director)
        {
            var rawScrewGroups = new List<Guid>[ScrewGroupCount];
            for (var i = 0; i < rawScrewGroups.Length; i++)
            {
                rawScrewGroups[i] = new List<Guid>();
            }

            var implantComponent = new ImplantCaseComponent();
            var objectManager = new CMFObjectManager(director);

            var screwBarrelRegistration =
                NeedBarrelRegistration ? new CMFBarrelRegistrator(director) : null;

            foreach (var screwComponentsForCase in Screws)
            {
                var caseGuid = screwComponentsForCase.Key;
                var screwComponents = screwComponentsForCase.Value;
                var casePreferenceDataModel =
                    director.CasePrefManager.CasePreferences.First(c => c.CaseGuid == caseGuid);
                
                var screwBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePreferenceDataModel);

                foreach (var screwComponent in screwComponents)
                {
                    var screw = screwComponent.GetScrew(director, casePreferenceDataModel, out var groupIndex);

                    var id = objectManager.AddNewBuildingBlock(screwBuildingBlock, screw);
                    screw.UpdateAidesInDocument();
                    rawScrewGroups[groupIndex].Add(id);
                }

                if (NeedRecalibration)
                {
                    RecalibrationScrewForTheCase(director, casePreferenceDataModel);
                }

                //no calibration at the moment
                screwBarrelRegistration?.RegisterScrewsBarrel(casePreferenceDataModel, null, false, out _);
            }

            screwBarrelRegistration?.Dispose();

            if (NeedBarrelRegistration)
            {
                director.GuidePhaseStarted = true;
            }

            foreach (var rawScrewGroup in rawScrewGroups)
            {
                director.ScrewGroups.Groups.Add(new ScrewManager.ScrewGroup(rawScrewGroup));
            }
        }

        public void FillToComponent(CMFImplantDirector director)
        {
            ScrewGroupCount = director.ScrewGroups.Groups.Count;

            var screwManager = new ScrewManager(director);

            foreach (var casePreferencesDataModel in director.CasePrefManager.CasePreferences)
            {
                var screws = screwManager.GetScrews(casePreferencesDataModel, false);
                var screwComponents = new List<ImplantScrewComponent>();

                foreach (var screw in screws)
                {
                    var screwComponent = new ImplantScrewComponent();
                    screwComponent.SetScrew(screw, director.ScrewGroups);
                    screwComponents.Add(screwComponent);
                }

                Screws.Add(casePreferencesDataModel.CaseGuid, screwComponents);
            }
        }
    }
}
