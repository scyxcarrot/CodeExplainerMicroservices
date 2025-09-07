using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.Factory;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.CMF.V2.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("83D64755-31F6-4D48-B726-ABE49740A757")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Planning, IBB.ProPlanImport)]
    public class CMF_TestChangeScrewBrand : CmfCommandBase
    {
        public CMF_TestChangeScrewBrand()
        {
            Instance = this;
        }

        public static CMF_TestChangeScrewBrand Instance { get; private set; }

        public override string EnglishName => "CMF_TestChangeScrewBrand";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var go = new GetOption();
            go.SetCommandPrompt("Choose Screw Brand.");
            go.AcceptNothing(true);

            var screwBrandOptions = new List<EScrewBrand> { EScrewBrand.Synthes, EScrewBrand.MtlsStandardPlus, EScrewBrand.SynthesUsCanada };
            go.AddOptionEnumSelectionList("Screw Brand", screwBrandOptions, 0);

            var screwBrand = director.CasePrefManager.SurgeryInformation.ScrewBrand;

            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    return Result.Cancel;
                }

                if (res == GetResult.Option)
                {
                    screwBrand = screwBrandOptions[go.Option().CurrentListOptionIndex];
                    continue;
                }

                if (res == GetResult.Nothing)
                {
                    break;
                }
            }

            if (screwBrand == director.CasePrefManager.SurgeryInformation.ScrewBrand)
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected screw brand is same as current screw brand: {screwBrand}");
                return Result.Cancel;
            }

            var dialog = new FolderBrowserDialog();
            dialog.Description = "Select a folder to save the new 3dm";
            var rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                return Result.Cancel;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            ChangeScrewBrand(director, screwBrand);

            //save and exit Rhino
            var dmProjectFile = Path.Combine(folderPath, $"{Path.GetFileNameWithoutExtension(doc.Name)}_{screwBrand}.3dm");
            var options = new Rhino.FileIO.FileWriteOptions();
            if (!doc.WriteFile(dmProjectFile, options))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not create project file.");
                return Result.Failure;
            }

            MessageBox.Show("Screw brand has been changed." +
                "\nImplants and guides with types that are not available in specified screw brand are deleted." +
                "\nImplant plate thickness, plate width, link width, screw type and pastille diameter are updated." +
                "\nPlanning implants are updated." +
                "\nImplant screws are deleted. Please enter Implant phase to have them regenerated." +
                "\nLandmarks are deleted. Please re-place them if needed." +
                "\nImplants and guides are unlinked. Please re-link them if needed." +
                "\nGuide fixation screws are deleted. Please re-place them if needed." +
                "\nImplant previews and guide previews are deleted. Please regenerate them.",
                "Change Screw Brand", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            SystemTools.OpenExplorerInFolder(folderPath);
            SystemTools.DiscardChanges();
            CloseAppAfterCommandEnds = true;
            return Result.Success;
        }

        private void ChangeScrewBrand(CMFImplantDirector director, EScrewBrand selectedScrewBrand)
        {       
            var surgeryInformation = new SurgeryInformationData();
            surgeryInformation.ScrewBrand = selectedScrewBrand;

            //set compulsory fields
            surgeryInformation.SurgeryInfoSurgeryApproach = "Coronal";
            surgeryInformation.IsFormerRemoval = false;
            surgeryInformation.IsSawThicknessSpecified = false;

            var screwBrandCasePreferences = CasePreferencesHelper.LoadScrewBrandCasePreferencesInfo(selectedScrewBrand);
            var implantTypes = screwBrandCasePreferences.Implants.Select(i => i.ImplantType);
            var screwLengthsPreferences = CasePreferencesHelper.LoadScrewLengthData();

            //delete implant/guide preferences that it's implant/guide type not available in selectedScrewBrand
            var implantsToBeDeleted = director.CasePrefManager.CasePreferences.Where(c => !implantTypes.Contains(c.CasePrefData.ImplantTypeValue)).ToList();
            foreach (var casePreference in implantsToBeDeleted)
            {
                var guid = casePreference.CaseGuid;
                RhinoApp.RunScript($"_-CMFDeleteImplant {guid}", false);
            }

            var guidesToBeDeleted = director.CasePrefManager.GuidePreferences.Where(g => !implantTypes.Contains(g.GuidePrefData.GuideTypeValue)).ToList();
            foreach (var guidePreference in guidesToBeDeleted)
            {
                var guid = guidePreference.CaseGuid;
                RhinoApp.RunScript($"_-CMFDeleteGuide PreferenceId {guid}", true);
            }

            director.CasePrefManager.SurgeryInformation = surgeryInformation;

            var implantComponent = new ImplantCaseComponent();
            var objectManager = new CMFObjectManager(director);
            var planningImplantBrepFactory = new PlanningImplantBrepFactory();

            foreach (var casePreference in director.CasePrefManager.CasePreferences)
            {
                var casePrefData = casePreference.CasePrefData;
                var implantType = casePrefData.ImplantTypeValue;
                var implant = screwBrandCasePreferences.Implants.Where(impl => impl.ImplantType == implantType).FirstOrDefault();
                var screwTypesList = implant.Screw.Select(screw => screw.ScrewType);

                casePrefData.SurgicalApproach = implant.SurgicalApproach.First();
                casePrefData.PlateThicknessMm = implant.PlateThickness;
                casePrefData.PlateWidthMm = implant.PlateWidth;
                casePrefData.LinkWidthMm = implant.LinkWidth;
                casePrefData.ScrewTypeValue = screwTypesList.First();
                casePrefData.PastilleDiameter = implant.Screw.Where(screw => screw.ScrewType == casePrefData.ScrewTypeValue).FirstOrDefault().PastilleDiameter;
                casePrefData.ScrewStyle = Queries.GetDefaultScrewStyleName(casePrefData.ScrewTypeValue);

                var screwLengthList = screwLengthsPreferences.ScrewLengths.Where(screw => screw.ScrewType == casePrefData.ScrewTypeValue).FirstOrDefault();
                if (director.CasePrefManager.SurgeryInformation.SurgeryType == ESurgeryType.Orthognathic)
                {
                    casePrefData.ScrewLengthMm = screwLengthList.DefaultOrthognathic;
                }
                else
                {
                    casePrefData.ScrewLengthMm = screwLengthList.DefaultReconstruction;
                }

                if (implant.ScrewFixationMain != null)
                {
                    casePrefData.IsScrewFixationTypeValueNA = true;
                    casePrefData.ScrewFixationTypeValue = "N/A";
                }
                else
                {
                    casePrefData.IsScrewFixationTypeValueNA = false;
                    casePrefData.ScrewFixationTypeValue = "";
                }

                if (implant.ScrewFixationRemaining != null)
                {
                    casePrefData.IsScrewFixationSkullRemainingTypeValueNA = true;
                    casePrefData.ScrewFixationSkullRemainingTypeValue = "N/A";
                }
                else
                {
                    casePrefData.IsScrewFixationSkullRemainingTypeValueNA = false;
                    casePrefData.ScrewFixationSkullRemainingTypeValue = "";
                }

                if (implant.ScrewFixationGraft != null)
                {
                    casePrefData.IsScrewFixationSkullGraftTypeValueNA = true;
                    casePrefData.ScrewFixationSkullGraftTypeValue = "N/A";
                }
                else
                {
                    casePrefData.IsScrewFixationSkullGraftTypeValueNA = false;
                    casePrefData.ScrewFixationSkullGraftTypeValue = "";
                }

                var implantDataModel = casePreference.ImplantDataModel;
                if (implantDataModel != null)
                {
                    //dot list - DotPastille
                    //connection list - ConnectionPlate, ConnectionLink
                    var dotList = implantDataModel.DotList;
                    var connectionList = implantDataModel.ConnectionList;

                    //update PlateThickness, PlateWidth, LinkWidth, PastilleDiameter
                    //unlink Screw, Landmark
                    foreach (var dot in dotList)
                    {
                        if (dot is DotPastille)
                        {
                            var pastille = (DotPastille)dot;
                            pastille.Thickness = casePrefData.PlateThicknessMm;
                            pastille.Diameter = casePrefData.PastilleDiameter;
                            pastille.Landmark = null;
                            pastille.Screw = null;
                        }
                    }

                    foreach (var connection in connectionList)
                    {
                        if (connection is ConnectionPlate)
                        {
                            var plate = (ConnectionPlate)connection;
                            plate.Thickness = casePrefData.PlateThicknessMm;
                            plate.Width = casePrefData.PlateWidthMm;
                        }
                        else if (connection is ConnectionLink)
                        {
                            var link = (ConnectionLink)connection;
                            link.Thickness = casePrefData.PlateThicknessMm;
                            link.Width = casePrefData.LinkWidthMm;
                        }
                    }

                    //update PlanningImplant
                    var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PlanningImplant, casePreference);
                    if (objectManager.HasBuildingBlock(buildingBlock))
                    {
                        var planning = planningImplantBrepFactory.CreateImplant(implantDataModel);
                        var oldImplantGuid = objectManager.GetBuildingBlockId(buildingBlock);
                        objectManager.SetBuildingBlock(buildingBlock, planning, oldImplantGuid);
                    }
                }
            }

            foreach (var guidePreference in director.CasePrefManager.GuidePreferences)
            {
                var guidePrefData = guidePreference.GuidePrefData;
                var guideType = guidePrefData.GuideTypeValue;
                var guide = screwBrandCasePreferences.Implants.Where(impl => impl.ImplantType == guideType).FirstOrDefault();

                if (guide.GuideScrews != null)
                {
                    guidePrefData.GuideScrewTypeValue = guide.GuideScrews.First();
                    guidePrefData.GuideScrewStyle = Queries.GetDefaultScrewStyleName(guidePrefData.GuideScrewTypeValue);
                }
                else
                {
                    guidePrefData.GuideScrewTypeValue = "";
                    guidePrefData.GuideScrewStyle = "";
                }

                if (guide.GuideCutSlot != null)
                {
                    guidePrefData.GuideCutSlotValue = guide.GuideCutSlot.First();
                }
                else
                {
                    guidePrefData.GuideCutSlotValue = "";
                }

                if (guide.GuideConnections != null)
                {
                    guidePrefData.GuideConnectionsValue = guide.GuideConnections.First();
                }
                else
                {
                    guidePrefData.GuideConnectionsValue = "";
                }

                guidePreference.LinkedImplantScrews.Clear();
            }

            //delete all related building blocks
            //implants
            var componentIds = objectManager.GetAllBuildingBlockIds(IBB.Screw).ToList();
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.Landmark));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.RegisteredBarrel));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.PastillePreview));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.ImplantPreview));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.ActualImplant));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.ActualImplantWithoutStampSubtraction));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.ActualImplantSurfaces));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.ActualImplantImprintSubtractEntity));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.ImplantScrewIndentationSubtractEntity));

            //guides
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.GuideFixationScrew));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.GuideFixationScrewEye));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.GuideFixationScrewLabelTag));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.GuidePreviewSmoothen));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.ActualGuide));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.GuideBaseWithLightweight));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.SmoothGuideBaseSurface));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.ActualGuideImprintSubtractEntity));
            componentIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.GuideScrewIndentationSubtractEntity));
            foreach (var id in componentIds)
            {
                objectManager.DeleteObject(id);
            }

            RhinoLayerUtilities.DeleteEmptyLayers(director.Document);
        }
    }
#endif
}
