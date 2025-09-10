using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using RhinoMtlsCore.Operations;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)

    [System.Runtime.InteropServices.Guid("bd71babf-88fc-4f30-b7c9-425a10b6e5a9")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant | DesignPhase.Guide)]
    public class CMF_TestCreateActualImplant : CmfCommandBase
    {
        static CMF_TestCreateActualImplant _instance;
        public CMF_TestCreateActualImplant()
        {
            _instance = this;
        }

        ///<summary>The only instance of the MyCommand1 command.</summary>
        public static CMF_TestCreateActualImplant Instance => _instance;

        public override string EnglishName => "CMF_TestCreateActualImplant";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var folderPath = string.Empty;

            if (IDSPluginHelper.ScriptMode)
            {
                var result = RhinoGet.GetString("FolderPath", true, ref folderPath);
                if (result != Result.Success || string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid folder path: {folderPath}");
                    return Result.Failure;
                }
            }
            else
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

                folderPath = Path.GetFullPath(dialog.SelectedPath);
            }

            var parameter = ImplantCreatorHelper.CreateImplantCreatorParams(director);

            var implantCreator = new ImplantCreator(director)
            { IsCreateActualImplant = true };
            var success = implantCreator.GenerateAllImplantPreviews(parameter);

            if (!success)
            {
                return Result.Failure;
            }


            var implants = implantCreator.GeneratedImplants;
            implants.ForEach(x =>
            {
                var color = CasePreferencesHelper.GetColor(x.Key.NCase);
                var meshColor = new int[3] {color.R, color.G, color.B};
                StlUtilities.RhinoMesh2StlBinary(x.Value.FinalImplant, Path.Combine(folderPath, $"{x.Key.CaseName}.stl"), meshColor);

                IDSPluginHelper.WriteLine(LogCategory.Default, $"{x.Key.CaseName} {x.Value.TotalTime.ToInvariantCultureString()} seconds");
                IDSPluginHelper.WriteLine(LogCategory.Default, $"Fixing of {x.Key.CaseName} {x.Value.FixingTime.ToInvariantCultureString()} seconds");

                var pastilleCylinders = implantCreator.PastilleCylinders.FirstOrDefault(y => y.Key == x.Key);

                StlUtilities.RhinoMesh2StlBinary(pastilleCylinders.Value, Path.Combine(folderPath, $"{x.Key.CaseName}_PastilleCylinders.stl"), new int[3] { color.G, color.G, color.G });

                var imprintSubtractEntities = implantCreator.GeneratedImplantsImprintSubtractEntities.FirstOrDefault(y => y.Key == x.Key);
                StlUtilities.RhinoMesh2StlBinary(imprintSubtractEntities.Value.Item1, Path.Combine(folderPath, $"{x.Key.CaseName}_ImprintSubtractEntities.stl"));

                var screwIndentationSubtractEntities = implantCreator.GeneratedImplantsScrewIndentationSubtractEntities.FirstOrDefault(y => y.Key == x.Key);
                StlUtilities.RhinoMesh2StlBinary(screwIndentationSubtractEntities.Value.Item1, Path.Combine(folderPath, $"{x.Key.CaseName}_ScrewIndentationSubtractEntities.stl"));

                var results = MeshDiagnostics.GetMeshDiagnostics(x.Value.FinalImplant);
                RhinoApp.WriteLine();
                RhinoApp.WriteLine($"MeshDiagnostics: {x.Key.CaseName}");
                RhinoApp.WriteLine($"NumberOfInvertedNormal = {results.NumberOfInvertedNormal}");
                RhinoApp.WriteLine($"NumberOfBadEdges = {results.NumberOfBadEdges}");
                RhinoApp.WriteLine($"NumberOfBadContours = {results.NumberOfBadContours}");
                RhinoApp.WriteLine($"NumberOfNearBadEdges = {results.NumberOfNearBadEdges}");
                RhinoApp.WriteLine($"NumberOfHoles = {results.NumberOfHoles}");
                RhinoApp.WriteLine($"NumberOfShells = {results.NumberOfShells}");
                RhinoApp.WriteLine($"NumberOfOverlappingTriangles = {results.NumberOfOverlappingTriangles}");
                RhinoApp.WriteLine($"NumberOfIntersectingTriangles = {results.NumberOfIntersectingTriangles}");
            });

            return Result.Success;
        }

    }

#endif
}
