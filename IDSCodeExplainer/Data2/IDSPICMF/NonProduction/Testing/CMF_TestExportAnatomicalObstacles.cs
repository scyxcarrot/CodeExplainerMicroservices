using System.Collections.Generic;
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

    [System.Runtime.InteropServices.Guid("6919b376-8533-4c7f-8ebe-5fcc760c8861")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.AnatomicalObstacles)]
    public class CMF_TestExportAnatomicalObstacles : CmfCommandBase
    {
        public CMF_TestExportAnatomicalObstacles()
        {
            Instance = this;
        }

        public static CMF_TestExportAnatomicalObstacles Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportAnatomicalObstacles";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var folderPath = string.Empty;

            if (mode == RunMode.Scripted)
            {
                //skip prompts and get folder path from command line
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
                dialog.Description = "Select a folder to export the anatomical obstacles";
                var rc = dialog.ShowDialog();
                if (rc != DialogResult.OK)
                {
                    return Result.Cancel;
                }
                folderPath = Path.GetFullPath(dialog.SelectedPath);
            }

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            var objectManager = new CMFObjectManager(director);
            var anatomicalObstaclesRhinoObjs = objectManager.GetAllBuildingBlocks(IBB.AnatomicalObstacles);

            List<Mesh> anatomicalObstacleMeshes = new List<Mesh>();
            foreach (var anatomicalObstaclesRhinoObj in anatomicalObstaclesRhinoObjs)
            {
                var meshes = anatomicalObstaclesRhinoObj.GetMeshes(MeshType.Default);

                foreach (var mesh in meshes)
                {
                    anatomicalObstacleMeshes.Add(mesh);
                }
            }

            var anatomicalObstacleFinalMesh = MeshUtilities.AppendMeshes(anatomicalObstacleMeshes);
            StlUtilities.RhinoMesh2StlBinary(anatomicalObstacleFinalMesh, $"{folderPath}\\anatomical_obstacles.stl");
            
            return Result.Success;
        }
    }
#endif
}
