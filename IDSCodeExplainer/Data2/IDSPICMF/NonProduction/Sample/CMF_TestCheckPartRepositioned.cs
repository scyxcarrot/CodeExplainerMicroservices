using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Importer;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
    #if (INTERNAL)

    [System.Runtime.InteropServices.Guid("6BD4F82E-2510-4DA5-9DBF-0E72C4D20B94")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.ProPlanImport)]
    public class CMF_TestCheckPartRepositioned : CmfCommandBase
    {
        public CMF_TestCheckPartRepositioned()
        {
            Instance = this;
        }

        public static CMF_TestCheckPartRepositioned Instance { get; private set; }

        public override string EnglishName => "CMF_TestCheckPartRepositioned";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var filePaths = StlImporter.SelectStlFiles(true, "Please select all the STL files. The file name will be used to retrieve part from opened case.");
            if (filePaths == null)
            {
                return Result.Failure;
            }

            var proPlanImportComponent = new ProPlanImportComponent();
            var objectManager = new CMFObjectManager(director); 
            var checker = new MeshRepositionedChecker();
            var loggings = new List<string>();

            foreach (var filePath in filePaths)
            {
                var partName = Path.GetFileNameWithoutExtension(filePath);

                if (!proPlanImportComponent.IsBlockRequired(partName))
                {
                    loggings.Add($"{partName}: Not a recognized part!");
                    continue;
                }

                var block = proPlanImportComponent.GetProPlanImportBuildingBlock(partName);
                var rhinoObjects = objectManager.GetAllBuildingBlocks(block).ToList();
                if (!rhinoObjects.Any())
                {
                    loggings.Add($"{partName}: Not found in case!");
                    continue;
                }

                var read = StlUtilities.StlBinary2RhinoMesh(filePath, out var meshToCompare);
                if (!read)
                {
                    loggings.Add($"{partName}: Error while reading STL file!");
                    continue;
                }

                var rhinoObject = rhinoObjects.First();
                var meshInCase = (Mesh)rhinoObject.Geometry.Duplicate();
                var isRepositioned = checker.IsMeshRepositioned(meshInCase, meshToCompare);
                loggings.Add($"{partName}: IsRepositioned={isRepositioned}");
            }

            foreach (var logging in loggings)
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, logging);
            }

            return Result.Success;
        }
    }

#endif
}
