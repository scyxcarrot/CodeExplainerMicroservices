using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.IO;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("F0B58951-5160-4A24-BF95-3A89677C218B")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.ProPlanImport)]
    public class CMF_TestExportRecutParts : CmfCommandBase
    {
        public CMF_TestExportRecutParts()
        {
            Instance = this;
        }

        public static CMF_TestExportRecutParts Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportRecutParts";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Select a folder to export all the recut parts in STL format";
            var rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                return Result.Cancel;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();
            var partsCount = 0;

            var proPlanImports = objectManager.GetAllBuildingBlocks(IBB.ProPlanImport);
            foreach (var part in proPlanImports)
            {
                var isRecut = part.Attributes.UserDictionary.ContainsKey(AttributeKeys.KeyIsRecut);
                if (!isRecut)
                {
                    continue;
                }

                partsCount++;
                var partName = proPlanImportComponent.GetPartName(part.Name);
                StlUtilities.RhinoMesh2StlBinary((Mesh)part.Geometry, $@"{folderPath}\{partName}.stl");
            }

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Total recut parts: {partsCount}");
            SystemTools.OpenExplorerInFolder(folderPath);

            return Result.Success;
        }
    }

#endif
}
