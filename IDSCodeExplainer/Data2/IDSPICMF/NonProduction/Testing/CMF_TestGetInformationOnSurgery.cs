#if (STAGING)
using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.Core.Utilities;
using IDS.PICMF.Forms;
using Newtonsoft.Json;
using Rhino;
using Rhino.Commands;
using System.IO;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("0FA5A110-A116-45D8-B6DE-E83FDF45870F")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestGetInformationOnSurgery : CmfCommandBase
    {
        private static CMF_TestGetInformationOnSurgery _instance;
        public CMF_TestGetInformationOnSurgery()
        {
            _instance = this;
        }

        public static CMF_TestGetInformationOnSurgery Instance => _instance;
        public override string EnglishName => "CMF_TestGetInformationOnSurgery";
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            var fileName = "InformationOnSurgery.json";
            var jsonPath = Path.Combine(workingDir, fileName);

            var informationOnSurgeryModel = new InformationOnSurgeryModel(director);
            var success = ExportInformationOnSurgeryJson(jsonPath, informationOnSurgeryModel);

            success = ExportMetalTeethIntegrationInformationJsons(workingDir, director);

            var openedFolder = SystemTools.OpenExplorerInFolder(workingDir);
            if (!openedFolder)
            {
                return Result.Failure;
            }

            return success? Result.Success : Result.Failure;
        }

        private bool ExportInformationOnSurgeryJson(string path, InformationOnSurgeryModel informationOnSurgeryModel)
        {
            using (var file = File.CreateText(path))
            {
                var json = JsonConvert.SerializeObject(informationOnSurgeryModel, Formatting.Indented);
                if (string.IsNullOrEmpty(json))
                {
                    return false;
                }

                file.Write(json);
            }

            return true;
        }

        private bool ExportMetalTeethIntegrationInformationJsons(string workingDir, CMFImplantDirector director)
        {
            var fileName = "MetalTeethIntegrationInformation.json";

            using (var file = File.CreateText(Path.Combine(workingDir, $"ImplantSupport{fileName}")))
            {
                var json = JsonConvert.SerializeObject(director.ImplantManager.GetImplantSupportRoICreationDataModel(), Formatting.Indented);
                if (string.IsNullOrEmpty(json))
                {
                    return false;
                }

                file.Write(json);
            }

            using (var file = File.CreateText(Path.Combine(workingDir, $"GuideSupport{fileName}")))
            {
                var json = JsonConvert.SerializeObject(director.GuideManager.GetGuideSupportRoICreationDataModel(), Formatting.Indented);
                if (string.IsNullOrEmpty(json))
                {
                    return false;
                }

                file.Write(json);
            }

            return true;
        }
    }
}

#endif