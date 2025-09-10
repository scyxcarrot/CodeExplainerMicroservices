using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.Logics;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Interface.Logic;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.IO;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("77EADF14-E2CB-4B6D-880C-B08259244416")]
    [IDSCMFCommandAttributes(DesignPhase.Guide | DesignPhase.TeethBlock)]
    public class CMFImportTeethBlock : CmfCommandBase
    {
        public CMFImportTeethBlock()
        {
            TheCommand = this;
        }

        public static CMFImportTeethBlock TheCommand { get; private set; }

        public override string EnglishName => "CMFImportTeethBlock";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var guideCaseGuid = GuidePreferencesHelper.PromptForPreferenceId();

            if (guideCaseGuid == Guid.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Guide preference not found!");
                return Result.Failure;
            }

            var success = PromptForFilepath(out var importFilePath);
            if (!success)
            {
                return Result.Failure;
            }

            var objectManager = new CMFObjectManager(director);
            var guidePrefModel = objectManager.GetGuidePreference(guideCaseGuid);
            var guideCaseComponent = new GuideCaseComponent();
            var buildingBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.TeethBlock, guidePrefModel);

            var idsConsole = new IDSRhinoConsole();
            var logicHelper = new ImportTeethBlockHelper(
                director, idsConsole, buildingBlock, importFilePath);
            var logic = new ImportTeethBlockLogic(idsConsole, logicHelper);
            var status = logic.Execute(out _);

            if (status == LogicStatus.Success)
            {
                // delete the teethBaseRegion
                // which will cascade and delete the teethBlock if it is IDS generated
                var teethBaseRegionEIbb =
                    guideCaseComponent.GetGuideBuildingBlock(
                        IBB.TeethBaseRegion, guidePrefModel);
                DeleteTeethBaseRegionIfPresent(director, teethBaseRegionEIbb);

                guidePrefModel.Graph.InvalidateGraph();
                guidePrefModel.Graph.NotifyBuildingBlockHasChanged(
                    new[] { IBB.TeethBlock });
            }

            return status.ToResultStatus();
        }

        private static void DeleteTeethBaseRegionIfPresent(
            CMFImplantDirector director,
            ExtendedImplantBuildingBlock teethBaseRegionEIbb)
        {
            var objectManager = new CMFObjectManager(director);
            if (!objectManager.HasBuildingBlock(teethBaseRegionEIbb))
            {
                return;
            }
            var teethBaseRegionIds =
                objectManager.GetAllBuildingBlockIds(teethBaseRegionEIbb);
            director.IdsDocument.Delete(teethBaseRegionIds);
        }

        private bool PromptForFilepath(out string importFilePath)
        {
            importFilePath = string.Empty;
            var success = false;
            var gm = new GetOption();
            gm.AcceptNothing(false);
            var modeImportFilePath = gm.AddOption("TeethBlockFilePath");
            while (true)
            {
                var getResult = gm.Get();
                if (getResult == GetResult.Cancel)
                {
                    break;
                }

                if (getResult != GetResult.Option)
                {
                    continue;
                }

                if (gm.OptionIndex() != modeImportFilePath)
                {
                    continue;
                }
                success = GetFilePath(out importFilePath);
                break;
            }

            return success;

        }

        private bool GetFilePath(out string importFilePath)
        {
            importFilePath = string.Empty;
            var result = RhinoGet.GetString("TeethBlockFilePath", true, ref importFilePath);
            if (result == Result.Nothing)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Interactive mode - please select the file path");
                return true;
            }

            if (result != Result.Success)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Path cannot be parsed {importFilePath}");
                return false;
            }

            if (!File.Exists(importFilePath))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"File does not exist {importFilePath}");
                return false;
            }

            return true;
        }
    }
}