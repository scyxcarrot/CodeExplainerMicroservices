using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.Graph;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("9CB06CC8-02B3-42BB-AB2A-15B539611697")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.Screw)]
    public class CMF_TestRecalibrateImplantScrew : CmfCommandBase
    {
        public CMF_TestRecalibrateImplantScrew()
        {
            TheCommand = this;
        }

        public static CMF_TestRecalibrateImplantScrew TheCommand { get; private set; }
        
        public override string EnglishName => "CMF_TestRecalibrateImplantScrew";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var getOption = new GetOption();
            getOption.SetCommandPrompt("Choose to calibrate the screws with RoI Volume or LoD");
            var fullLoD = getOption.AddOption("FullLoD");
            getOption.AddOption("LowLoD");
            getOption.EnableTransparentCommands(false);
            getOption.Get();

            if (getOption.CommandResult() != Result.Success)
            {
                return getOption.CommandResult();
            }

            var option = getOption.Option();
            if (option == null)
            {
                return Result.Failure;
            }

            var objectManager = new CMFObjectManager(director);
            var implantSupportManager = new ImplantSupportManager(objectManager);
            var screws = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(obj => obj as Screw);
            var optionSelected = option.Index;

            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                var actualSupportRhObject = implantSupportManager.GetImplantSupportRhObj(casePreferenceData);
                if (actualSupportRhObject == null)
                {
                    continue;
                }

                Mesh calibrationMesh;
                var calibrationMeshDescription = string.Empty;
                if (optionSelected == fullLoD)
                {
                    calibrationMesh = ImplantCreationUtilities.GetImplantRoIVolumeWithoutCheck(objectManager, casePreferenceData,
                        ref actualSupportRhObject);
                    calibrationMeshDescription = "full LoD(RoI volume)";
                }
                else
                {
                    objectManager.GetBuildingBlockLoDLow(actualSupportRhObject.Id, out calibrationMesh);
                    calibrationMeshDescription = "low LoD";
                }

                IDSPluginHelper.WriteLine(LogCategory.Default, $"Calibrating implant screws for {casePreferenceData.CaseName} with" +
                                                               $" {calibrationMeshDescription} support mesh");

                var pastillePreviewIds = new List<Guid>();
                var dots = new List<IDot>();

                foreach (var pastille in casePreferenceData.ImplantDataModel.DotList.Select(d => d as DotPastille))
                {
                    if (pastille == null)
                    {
                        continue;
                    }

                    var screw = screws.FirstOrDefault(s => s.Id == pastille.Screw.Id);

                    var headPoint = RhinoPoint3dConverter.ToPoint3d(pastille.Location);
                    var length = (screw.HeadPoint - screw.TipPoint).Length;
                    var tipPoint = headPoint + screw.Direction * length;

                    // Check if leveling can be done before replacing the old screw by the updated screw
                    var newDraftScrew = new Screw(screw.Director,
                        headPoint,
                        tipPoint,
                        screw.ScrewAideDictionary, 
                        screw.Index, 
                        screw.ScrewType, 
                        screw.BarrelType);

                    var calibrator = new ScrewCalibrator(calibrationMesh);
                    if (calibrator.LevelHeadOnTopOfMesh(newDraftScrew, casePreferenceData.CasePrefData.PlateThicknessMm, true))
                    {
                        var calibratedScrew = calibrator.CalibratedScrew;

                        var screwManager = new ScrewManager(director);
                        screwManager.ReplaceExistingScrewInDocument(calibratedScrew, ref screw,
                            casePreferenceData, true);

                        var helper = new PastillePreviewHelper(director);
                        var pastillePreviewId = helper.GetPastillePreviewBuildingBlockId(casePreferenceData, pastille);

                        ScrewPastilleManager.UpdateScrewDataInPastille(pastille, calibratedScrew, false);

                        RegisteredBarrelUtilities.NotifyBuildingBlockHasChanged(director, calibratedScrew.Id);

                        director.ImplantManager.InvalidateConnectionBuildingBlock(casePreferenceData);
                        director.ImplantManager.InvalidateLandmarkBuildingBlock(casePreferenceData);

                        pastillePreviewIds.Add(pastillePreviewId);
                        dots.Add(pastille);
                    }
                }

                var connectionPreviewHelper = new ConnectionPreviewHelper(director);
                var connectionPreviewIds = connectionPreviewHelper.GetRhinoObjectIdsFromDots(casePreferenceData, dots);

                casePreferenceData.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.Screw }, new List<TargetNode>
                {
                    new TargetNode
                    {
                        Guids = pastillePreviewIds,
                        IBB = IBB.PastillePreview
                    },
                    new TargetNode
                    {
                        Guids = connectionPreviewIds,
                        IBB = IBB.ConnectionPreview
                    }
                },
                IBB.Landmark, IBB.Connection, IBB.RegisteredBarrel);
            }
            return Result.Success;
        }
    }
#endif
}