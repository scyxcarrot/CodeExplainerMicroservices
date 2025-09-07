using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)

    [System.Runtime.InteropServices.Guid("8C004310-00DA-4139-976F-CBC80F10E6E4")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.GuideSurfaceWrap)]
    public class CMF_TestCreateActualGuide : CmfCommandBase
    {
        public CMF_TestCreateActualGuide()
        {
            Instance = this;
        }
        
        public static CMF_TestCreateActualGuide Instance { get; private set; }

        public override string EnglishName => "CMF_TestCreateActualGuide";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "Select Destination to Export"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Aborted.");
                return Result.Failure;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);
            var diagnostics = new Dictionary<string, MeshDiagnostics.MeshDiagnosticsResult>();

            foreach (var guidePreference in director.CasePrefManager.GuidePreferences)
            {
                GuideCreatorV2.InputMeshesInfo inputMeshInfo;
                bool isNeedManualQprt;
                var guide = GuideCreatorHelper.CreateActualGuide(director.Document, director, guidePreference, false, out inputMeshInfo, out isNeedManualQprt);
                if (guide != null)
                {
                    var results = MeshDiagnostics.GetMeshDiagnostics(guide);
                    diagnostics.Add(guidePreference.CaseName, results);

                    var color = CasePreferencesHelper.GetColor(guidePreference.NCase);
                    var meshColor = new int[3] { color.R, color.G, color.B };
                    StlUtilities.RhinoMesh2StlBinary(guide, Path.Combine(folderPath, $"{guidePreference.CaseName}.stl"), meshColor);
                }
            }
            
            foreach (var diagnostic in diagnostics)
            {
                RhinoApp.WriteLine(); 
                RhinoApp.WriteLine($"MeshDiagnostics: {diagnostic.Key}");
                var results = diagnostic.Value;
                RhinoApp.WriteLine($"NumberOfInvertedNormal = {results.NumberOfInvertedNormal}");
                RhinoApp.WriteLine($"NumberOfBadEdges = {results.NumberOfBadEdges}");
                RhinoApp.WriteLine($"NumberOfBadContours = {results.NumberOfBadContours}");
                RhinoApp.WriteLine($"NumberOfNearBadEdges = {results.NumberOfNearBadEdges}");
                RhinoApp.WriteLine($"NumberOfHoles = {results.NumberOfHoles}");
                RhinoApp.WriteLine($"NumberOfShells = {results.NumberOfShells}");
                RhinoApp.WriteLine($"NumberOfOverlappingTriangles = {results.NumberOfOverlappingTriangles}");
                RhinoApp.WriteLine($"NumberOfIntersectingTriangles = {results.NumberOfIntersectingTriangles}");
            }

            return Result.Success;
        }

    }

#endif
}
