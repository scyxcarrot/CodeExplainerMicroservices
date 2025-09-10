using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using IDS.RhinoInterfaces.Converter;
using Newtonsoft.Json;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.IO;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)
    [System.Runtime.InteropServices.Guid("660A65AE-09E9-4ED9-89AA-F6977A3077C2")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.ProPlanImport)]
    public class CMF_TestExportPartsTransformationMatrix : CmfCommandBase
    {
        public CMF_TestExportPartsTransformationMatrix()
        {
            Instance = this;
        }

        public static CMF_TestExportPartsTransformationMatrix Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportPartsTransformationMatrix";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var objectManager = new CMFObjectManager(director);
            var rhinoObjects = objectManager.GetAllBuildingBlocks(IBB.ProPlanImport);

            var list = new SmartDesignPartTransformationMatrixList();

            foreach (var rhinoObject in rhinoObjects)
            {
                list.ExportedParts.Add(new SmartDesignPartTransformationMatrix
                {
                    ExportedPartName = proPlanImportComponent.GetPartName(rhinoObject.Name),
                    TransformationMatrix = ((Transform)rhinoObject.Attributes.UserDictionary[AttributeKeys.KeyTransformationMatrix]).ToIDSTransform()
                });
            }

            var directory = DirectoryStructure.GetWorkingDir(doc);
            using (var file = File.CreateText($"{directory}\\coordinatesystems.json"))
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, list);
            }

            SystemTools.OpenExplorerInFolder(directory);

            return Result.Success;
        }
    }

#endif
}
