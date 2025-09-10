using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using System.IO;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)

    [System.Runtime.InteropServices.Guid("E093D8E6-6F6C-4E4C-BC70-DDFE478ECAF7")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.ReferenceEntities)]
    public class CMF_TestExportReferenceEntities : CmfCommandBase
    {
        public CMF_TestExportReferenceEntities()
        {
            Instance = this;
        }

        public static CMF_TestExportReferenceEntities Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportReferenceEntities";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var folderPath = string.Empty;

            if (mode == RunMode.Scripted)
            {
                var result = RhinoGet.GetString("FolderPath", false, ref folderPath);
                if (result != Result.Success || string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid folder path: {folderPath}");
                    return Result.Failure;
                }
            }
            else
            {
                var dialog = new FolderBrowserDialog();
                dialog.Description = "Select a folder to export the reference entities";
                var rc = dialog.ShowDialog();
                if (rc != DialogResult.OK)
                {
                    return Result.Cancel;
                }
                folderPath = Path.GetFullPath(dialog.SelectedPath);
            }

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            var objectManager = new CMFObjectManager(director);
            var referenceEntitiesRhinoObjs = objectManager.GetAllBuildingBlocks(IBB.ReferenceEntities);

            foreach (var referenceEntitiesRhinoObj in referenceEntitiesRhinoObjs)
            {
                var name = doc.Layers[referenceEntitiesRhinoObj.Attributes.LayerIndex].Name;
                var mesh = (Mesh)referenceEntitiesRhinoObj.Geometry;
                StlUtilities.RhinoMesh2StlBinary(mesh, $@"{folderPath}\{name}.stl");
            }
            
            return Result.Success;
        }
    }
#endif
}
