using IDS.CMF.DataModel;
using IDS.CMF.Operations;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Style = Rhino.Commands.Style;

namespace IDS.PICMF.NonProduction
{
    //this is a slight modification of CMFCreateGuideSupport command:
    //allow user to pass in a folder path where all the input RoI STLs are located
    //command will run guide support generation for each of the STLs

#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("3EF43EE4-6223-49F1-91FE-964248AD2AE9")]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_TestCreateGuideSupport : Command
    {
        public CMF_TestCreateGuideSupport()
        {
            TheCommand = this;
        }
        
        public static CMF_TestCreateGuideSupport TheCommand { get; private set; }
        
        public override string EnglishName => "CMF_TestCreateGuideSupport";

        public const double DefaultGCDForGuideSupportRoI = 4.0;
        public const double MinGCDForGuideSupportRoI = 4.0;
        public const double MaxGCDForGuideSupportRoI = 8.0;
        public const bool DefaultSW2ForGuideSupportRoI = false;

        public const double DefaultSDForGuideSupportRoI = 0.2; //temporary for testing
        public const double MinSDForGuideSupportRoI = 0.2;
        public const double MaxSDForGuideSupportRoI = 10.0;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var getOption = new GetOption();
            getOption.SetCommandPrompt("Change the parameter values and press enter.");
            getOption.AcceptNothing(true);
            getOption.EnableTransparentCommands(false);

            var optionGCD = new OptionDouble(DefaultGCDForGuideSupportRoI, MinGCDForGuideSupportRoI, MaxGCDForGuideSupportRoI);
            getOption.AddOptionDouble("GapClosingDistanceForWrapRoI1", ref optionGCD, $"Minimum: {MinGCDForGuideSupportRoI}, Maximum: {MaxGCDForGuideSupportRoI}");

            var optionSD = new OptionDouble(DefaultSDForGuideSupportRoI, MinSDForGuideSupportRoI, MaxSDForGuideSupportRoI);
            getOption.AddOptionDouble("SmallestDetailForWrapUnion", ref optionSD, $"Minimum: {MinSDForGuideSupportRoI}, Maximum: {MaxSDForGuideSupportRoI}");

            var optionSW = new OptionToggle(DefaultSW2ForGuideSupportRoI, "False", "True");
            getOption.AddOptionToggle("SkipWrap2", ref optionSW);

            while (true)
            {
                var getResult = getOption.Get();
                if (getResult == GetResult.Nothing)
                {
                    var dialog = new OpenFileDialog
                    {
                        Title = "Select a folder where input RoI STLs are located."
                    };
                    var rc = dialog.ShowDialog();
                    if (rc != DialogResult.OK)
                    {
                        return Result.Failure;
                    }

                    var directoryInfo = new FileInfo(dialog.FileName).Directory;
                    var stls = directoryInfo.GetFiles("*.stl");
                    if (!stls.Any())
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Error, $"No STL file found!");
                        return Result.Failure;
                    }

                    var folderPath = directoryInfo.FullName;

                    var selectedGcd = optionGCD.CurrentValue;
                    var currentGCD = Math.Round(selectedGcd, 1, MidpointRounding.AwayFromZero);
                    var currentSD = optionSD.CurrentValue;
                    var currentSW2 = optionSW.CurrentValue;

                    var success = true;

                    foreach (var file in stls)
                    {
                        Mesh inputMesh;
                        if (!StlUtilities.StlBinary2RhinoMesh(file.FullName, out inputMesh))
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to import {file.Name}!");
                            continue;
                        }

                        var dataModel = new SupportCreationDataModel();
                        dataModel.InputRoI = inputMesh;
                        dataModel.GapClosingDistanceForWrapRoI1 = currentGCD;
                        dataModel.SmallestDetailForWrapUnion = currentSD;
                        dataModel.SkipWrapRoI2 = currentSW2;

                        var logger = new StringBuilder();
                        logger.AppendLine("### Implant Design Suite Log ###");

                        PreviewRoIWrap1(ref dataModel, ref logger);
                        if (dataModel.WrapRoI1 == null)
                        {
                            IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to generate RoI Wrap 1 for {file.Name}!");
                            continue;
                        }

                        var res = GuideSupportCreation(dataModel, ref logger);
                        if (res == Result.Success)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(file.Name);
                            var exportDir = $@"{folderPath}\GuideSupportMeshGeneration-{fileName}";

                            ExportIntermediates(exportDir, dataModel);
                            DisplayFinalResultDiagnostics(dataModel.FinalResult, ref logger);

                            var logs = logger.ToString();
                            if (logger.Length > 0)
                            {
                                using (var outfile = new StreamWriter($@"{exportDir}\Report.txt", false))
                                {
                                    outfile.Write(logs);
                                }
                            }
                        }
                        else
                        {
                            success = false;
                        }
                    }

                    SystemTools.OpenExplorerInFolder(folderPath);

                    return success ? Result.Success : Result.Failure;
                }
                else if (getResult == GetResult.Cancel || getResult == GetResult.NoResult)
                {
                    return Result.Failure;
                }
            }
        }

        private void PreviewRoIWrap1(ref SupportCreationDataModel dataModel, ref StringBuilder logger)
        {
            var timer = new Stopwatch();
            timer.Start();

            var creator = new SupportCreator();
            creator.PerformRoIWrap1(ref dataModel);

            timer.Stop();

            var log = $"It took {timer.ElapsedMilliseconds * 0.001} seconds to create RoI wrap 1.";
            logger.AppendLine(log);
        }

        private Result GuideSupportCreation(SupportCreationDataModel dataModel, ref StringBuilder logger)
        {
            var timer = new Stopwatch();
            timer.Start(); 
            
            var creator = new SupportCreator();
            var success = creator.PerformSupportCreation(ref dataModel, out var performanceReport);

            timer.Stop();

            if (success)
            {
                var log = $"It took {timer.ElapsedMilliseconds * 0.001} seconds to create guide support. (*Note: Time taken only includes automation steps)";
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, log);
                logger.AppendLine(log);
            }
            else
            {
                var log = $"Failed to create guide support. Please try again later...";
                IDSPluginHelper.WriteLine(LogCategory.Error, log);
                logger.AppendLine(log);
            }

            logger.AppendLine($"InputRoI N Triangles: {dataModel.InputRoI?.Faces.Count.ToString()}");
            logger.AppendLine($"InputRoI N Vertices: {dataModel.InputRoI?.Vertices.Count.ToString()}");
            logger.AppendLine($"FinalResult N Triangles: {dataModel.FinalResult?.Faces.Count.ToString()}");
            logger.AppendLine($"FinalResult N Vertices: {dataModel.FinalResult?.Vertices.Count.ToString()}");
            logger.AppendLine($"GapClosingDistanceForWrapRoI1: {dataModel.GapClosingDistanceForWrapRoI1}");
            logger.AppendLine($"SkipWrapRoI2: {dataModel.SkipWrapRoI2}");
            logger.AppendLine($"SmallestDetailForWrapUnion: {dataModel.SmallestDetailForWrapUnion}");
            logger.AppendLine($"Result Status: {success}");

            return success ? Result.Success : Result.Failure;
        }

        private void ExportIntermediates(string exportDir, SupportCreationDataModel dataModel)
        {
            StlUtilities.RhinoMesh2StlBinary(dataModel.InputRoI, $"{exportDir}\\InputRoI.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.WrapRoI1, $"{exportDir}\\WrapRoI1-GCD{dataModel.GapClosingDistanceForWrapRoI1}.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.WrapRoI2, $"{exportDir}\\WrapRoI2-S{dataModel.SkipWrapRoI2}.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.UnionedMesh, $"{exportDir}\\UnionedMesh.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.WrapUnion, $"{exportDir}\\WrapUnion-SD{dataModel.SmallestDetailForWrapUnion}.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.RemeshedMesh, $"{exportDir}\\RemeshedMesh.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.SmoothenMesh, $"{exportDir}\\SmoothenMesh.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.FinalResult, $"{exportDir}\\FinalResult.stl");
        }

        private void DisplayFinalResultDiagnostics(Mesh mesh, ref StringBuilder logger)
        {
            var results = MeshDiagnostics.GetMeshDiagnostics(mesh);
            var log = $"MeshDiagnostics: FinalResult" + "\n" 
                + $"NumberOfInvertedNormal = {results.NumberOfInvertedNormal}" + "\n"
                + $"NumberOfBadEdges = {results.NumberOfBadEdges}" + "\n"
                + $"NumberOfBadContours = {results.NumberOfBadContours}" + "\n"
                + $"NumberOfNearBadEdges = {results.NumberOfNearBadEdges}" + "\n"
                + $"NumberOfHoles = {results.NumberOfHoles}" + "\n"
                + $"NumberOfShells = {results.NumberOfShells}" + "\n"
                + $"NumberOfOverlappingTriangles = {results.NumberOfOverlappingTriangles}";

            RhinoApp.WriteLine();
            RhinoApp.WriteLine(log);
            logger.AppendLine(log);
        }
    }
#endif
}