using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.Logics;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Geometry;
using IDS.Core.V2.Utilities;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using Newtonsoft.Json;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMF.Utilities
{
    public static class SmartDesignUtilities
    {
        public static bool SmartDesignOperation(ISmartDesignRecutModel dataModel, CMFImplantDirector director, out string outputPath)
        {
            outputPath = string.Empty;

            const string outputSuffix = "_recut";
            var cmfResource = new CMFResources();
            var batFileReturns = ExecuteBatFileWithReturn(cmfResource.AutoDeploymentCheckPBAVersionScriptPath);

            // Check if PBA_env exists
            if (batFileReturns.Any(p => p.Contains("No Python found")))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    "Could not find PBA_env. Please rerun the All-In-One installer to use the SmartDesign package.");

                return false;
            }

            if (!PrepareInputFiles(dataModel, director, out var inputTempPath, out var hasOsteotomyHandler))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Failed to export stl/json files");
                SystemTools.DeleteRecursively(inputTempPath);
                return false;
            }

#if (STAGING)
            // Export Input as Intermediates
            var dirName = $"RECUT_{(dataModel.WedgeOperation ? "WEDGE_" : "")}{dataModel.RecutType}_Intermediates";
            var filePath = Path.Combine(DirectoryStructure.GetWorkingDir(director.Document), dirName);

            ExportUtilities.MoveFilesToFolderWithNewSubdirectory(filePath, new List<Tuple<string, string>>
            {
                new Tuple<string, string>("Input", inputTempPath)
            }, true);
#endif

            outputPath = CreateOutputFolder(dataModel.RecutType, dataModel.WedgeOperation, director);

            var argument = PrepareSmartDesignCommand(dataModel, inputTempPath, outputSuffix, outputPath, hasOsteotomyHandler);

            var returnCodes = ExternalToolsUtilities.RunExternalToolWithCode(cmfResource.SmartDesignExecuteOperationScriptPath,
                argument, null, false, new IDSRhinoConsole());

            if (!ErrorHandling(returnCodes, outputPath))
            {
                SystemTools.DeleteRecursively(inputTempPath);

                // SmartDesign with an error code 2 does not create a log file.
                if (!Directory.EnumerateFileSystemEntries(outputPath).Any())
                {
                    SystemTools.DeleteRecursively(outputPath);
                }

                return false;
            }

#if !(INTERNAL)
            // Preserve log files when operation ran successfully in developer mode, delete them when in production
            foreach (var file in Directory.GetFiles(outputPath, "*.log"))
            {
                File.Delete(file);
            }
#endif

            // This might happen when process ends unexpectedly, without completing the operation
            if (!Directory.EnumerateFileSystemEntries(outputPath).Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not find results in the output folder. " +
                                                             "Please report this to the development team");
                SystemTools.DeleteRecursively(inputTempPath);
                SystemTools.DeleteRecursively(outputPath);
                return false;
            }

#if (STAGING)
            // Export Output as Intermediates
            var movedFiles = ExportUtilities.MoveFilesToExistingFolderWithNewSubdirectory(filePath, new List<Tuple<string, string>>
            {
                new Tuple<string, string>("Output", outputPath)
            });

            if (!movedFiles)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not find the directory!");
            }
#endif

            SystemTools.DeleteRecursively(inputTempPath);

            if (!DeleteSuffixInOutput(dataModel, outputPath, outputSuffix))
            {
                return false;
            }

            return true;
        }

        public static bool PrepareInputFiles(ISmartDesignRecutModel dataModel, CMFImplantDirector director, out string tempFolder, out bool hasOsteotomyHandler)
        {
            tempFolder = CreateTempFolder(SmartDesignStrings.OperationInputFolderName);
            return PrepareInputFiles(dataModel, director, tempFolder, out hasOsteotomyHandler);
        }

        public static bool PrepareInputFiles(ISmartDesignRecutModel dataModel, CMFImplantDirector director, string directory, out bool hasOsteotomyHandler)
        {
            var requiredPartNames = new List<string>();

            switch (dataModel.RecutType)
            {
                case SmartDesignOperations.RecutLefort:
                    var lefortDataModel = dataModel as SmartDesignLefortRecutModel;
                    requiredPartNames.AddRange(new List<string>() { lefortDataModel.Osteotomy, lefortDataModel.Maxilla, lefortDataModel.Skull });

                    if (lefortDataModel.PterygoidCuts.Any())
                    {
                        requiredPartNames.AddRange(lefortDataModel.PterygoidCuts);
                    }

                    if (lefortDataModel.WedgeOperation && !string.IsNullOrEmpty(lefortDataModel.SkullComplete))
                    {
                        requiredPartNames.Add(lefortDataModel.SkullComplete);
                    }

                    break;

                case SmartDesignOperations.RecutBSSO:
                    var bssoDataModel = dataModel as SmartDesignBSSORecutModel;
                    requiredPartNames.AddRange(bssoDataModel.Osteotomies);
                    requiredPartNames.AddRange(new List<string>() { bssoDataModel.Body, bssoDataModel.RamusR, bssoDataModel.RamusL });

                    if (bssoDataModel.WedgeOperation)
                    {
                        if (!string.IsNullOrEmpty(bssoDataModel.MandibleComplete))
                        {
                            requiredPartNames.Add(bssoDataModel.MandibleComplete);
                        }

                        if (!string.IsNullOrEmpty(bssoDataModel.MandibleTeeth))
                        {
                            requiredPartNames.Add(bssoDataModel.MandibleTeeth);
                        }

                        if (!string.IsNullOrEmpty(bssoDataModel.NerveR))
                        {
                            requiredPartNames.Add(bssoDataModel.NerveR);
                        }

                        if (!string.IsNullOrEmpty(bssoDataModel.NerveL))
                        {
                            requiredPartNames.Add(bssoDataModel.NerveL);
                        }
                    }

                    break;

                case SmartDesignOperations.RecutGenio:
                    var genioDataModel = dataModel as SmartDesignGenioRecutModel;
                    requiredPartNames.AddRange(new List<string> { genioDataModel.Osteotomy, genioDataModel.Mandible, genioDataModel.Chin });

                    if (genioDataModel.WedgeOperation && !string.IsNullOrEmpty(genioDataModel.MandibleComplete))
                    {
                        requiredPartNames.Add(genioDataModel.MandibleComplete);
                    }

                    break;

                case SmartDesignOperations.RecutSplitMax:
                    var splitMaxDataModel = dataModel as SmartDesignSplitMaxRecutModel;
                    requiredPartNames.AddRange(splitMaxDataModel.Osteotomies);
                    requiredPartNames.AddRange(splitMaxDataModel.MaxillaParts);

                    break;
            }

            hasOsteotomyHandler = ExportOsteotomyHandlerToTempFolder(requiredPartNames, director, directory);

            if (!ExportPartsToTempFolder(requiredPartNames, director, directory))
            {
                return false;
            }

            if (dataModel.WedgeOperation && !ExportTransformationMatricesToTempFolder(requiredPartNames, director, directory))
            {
                return false;
            }

            return true;
        }

        private static bool ErrorHandling(int errorCode, string outputPath)
        {
            var genericMessage = $"If issue still persist, please report this to the development team with the log file in: \n{outputPath}";

            switch (errorCode)
            {
                case SmartDesignReturnCodes.GeneralErrorCode:
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Please make sure that the inputs for the operation are valid. {genericMessage}");
                    return false;

                case SmartDesignReturnCodes.UnrecognisedErrorCode:
                    IDSPluginHelper.WriteLine(LogCategory.Error, "SmartDesign could not recognize the command. Please report this to the development team.");
                    return false;

                case SmartDesignReturnCodes.CommandError:
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Something went wrong while executing the command. Please retry the operation. {genericMessage}");
                    return false;

                case SmartDesignReturnCodes.DataReadError:
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Data could not be read. Please retry the operation. {genericMessage}");
                    return false;

                case SmartDesignReturnCodes.DataWriteError:
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Results could not be saved to location. Please retry the operation. {genericMessage}");
                    return false;

                case SmartDesignReturnCodes.SuccessCode:
                    IDSPluginHelper.WriteLine(LogCategory.Default, $"Successfully executed SmartDesign operation. Results written in: \n{outputPath}");
                    return true;
            }

            return true;
        }

        private static string CreateTempFolder(string subFolderName, bool deleteIfExist = true)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), subFolderName);

            try
            {
                if (deleteIfExist && Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }

                Directory.CreateDirectory(tempDirectory);
            }
            catch (Exception exception)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"Failed to create subfolder in temporary folder: {exception.Message}");
            }

            return tempDirectory;
        }

        private static string CreateOutputFolder(string operation, bool isWedgeOperation, CMFImplantDirector director)
        {
            var fileIncrement = 1;
            var workPath = DirectoryStructure.GetWorkingDir(director.Document);
            var dirName = $"RECUT_{(isWedgeOperation ? "WEDGE_" : "")}{operation}";
            var filePath = Path.Combine(workPath, dirName);

            while (Directory.Exists(filePath))
            {
                filePath = Path.Combine(workPath, $"{dirName}({fileIncrement})");
                fileIncrement++;
            }

            try
            {
                Directory.CreateDirectory(filePath);
            }
            catch (Exception exception)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"Failed to create output folder: {exception.Message}");
            }

            return filePath;
        }

        private static bool DeleteSuffixInOutput(ISmartDesignRecutModel dataModel, string outputPath, string outputSuffix)
        {
            try
            {
                var outputFiles = Directory.GetFiles(outputPath, "*.stl");

                foreach (var path in outputFiles)
                {
                    var name = Path.GetFileName(path);
                    File.Move(Path.Combine(outputPath, name),
                        Path.Combine(outputPath, name.Replace(outputSuffix, "")));
                }

                if (DeleteSplitSsoOsteotomySuffix(dataModel, outputPath))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Split SSO option detected, extra suffix from osteotomy removed!");
                }
            }
            catch (Exception exception)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"Failed to remove suffix in output folder: {exception.Message}");

                return false;
            }

            return true;
        }

        public static bool DeleteSplitSsoOsteotomySuffix(ISmartDesignRecutModel dataModel, string outputPath)
        {
            // We need to delete the extra suffix from BSSO's osteotomy due to the split-sso flag from the operation
            if (dataModel.RecutType == SmartDesignOperations.RecutBSSO)
            {
                var bssoDataModel = dataModel as SmartDesignBSSORecutModel;

                if (!bssoDataModel.SplitSso)
                {
                    return false;
                }

                var outputFiles = Directory.GetFiles(outputPath, "*.stl");

                foreach (var osteotomy in bssoDataModel.Osteotomies)
                {
                    var regex = new Regex(osteotomy);
                    var name = outputFiles.ToList().Where(o => regex.IsMatch(o) && !o.Contains("bone")).Select(Path.GetFileNameWithoutExtension).ToList();
                    
                    name.ForEach(n =>
                    {
                        File.Move(Path.Combine(outputPath, $"{n}.stl"),
                            Path.Combine(outputPath, $"{n.Substring(0, n.Length - 2)}.stl"));
                    });
                }
            }

            return true;
        }

        private static bool ExportPartsToTempFolder(List<string> requiredPartNames, CMFImplantDirector director, string tempFolder)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var objectManager = new CMFObjectManager(director);

            foreach (var partName in requiredPartNames)
            {
                if (partName == string.Empty)
                {
                    continue;
                }

                var block = proPlanImportComponent.GetProPlanImportBuildingBlock(partName);

                if (!objectManager.HasBuildingBlock(block))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error,
                        $"Could not find the part: {partName}");
                    return false;
                }

                var mesh = (Mesh)objectManager.GetBuildingBlock(block).Geometry;
                var path = Path.Combine(tempFolder, $"{partName}.stl");

                StlUtilitiesV2.IDSMeshToStlBinary(RhinoMeshConverter.ToIDSMesh(mesh), path);
            }

            return true;
        }

        public static bool ExportTransformationMatricesToTempFolder(List<string> requiredPartNames, CMFImplantDirector director, string tempFolder)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var objectManager = new CMFObjectManager(director);

            var list = new SmartDesignPartTransformationMatrixList();

            foreach (var partName in requiredPartNames)
            {
                if (partName == string.Empty)
                {
                    continue;
                }

                //export Planned part's transformation matrix
                var partNameWithoutSurgeryStage = ProPlanPartsUtilitiesV2.GetPartNameWithoutSurgeryStage(partName);
                var plannedObject = objectManager.GetAllBuildingBlockRhinoObjectByMatchingName(IBB.ProPlanImport, $"0[2-9]{partNameWithoutSurgeryStage}$").FirstOrDefault();
                if (plannedObject == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Could not find the Planned part of: {partName}");
                    continue;
                }

                var block = proPlanImportComponent.GetProPlanImportBuildingBlock(partName);
                var originalObject = objectManager.GetAllBuildingBlocks(block).ToList().First();
                var originalTransform = new Transform((Transform)originalObject.Attributes.UserDictionary[AttributeKeys.KeyTransformationMatrix]);
                var plannedTransform = new Transform((Transform)plannedObject.Attributes.UserDictionary[AttributeKeys.KeyTransformationMatrix]);

                if (originalTransform != Transform.Identity)
                {
                    // Original/PreOp part's transformation matrix should always be Identity
                    if (!originalTransform.TryGetInverse(out var inverseTrans))
                    {
                        return false;
                    }

                    plannedTransform = Transform.Multiply(plannedTransform, inverseTrans);
                }

                list.ExportedParts.Add(new SmartDesignPartTransformationMatrix
                {
                    ExportedPartName = proPlanImportComponent.GetPartName(plannedObject.Name),
                    TransformationMatrix = plannedTransform.ToIDSTransform()
                });
            }

            using (var file = File.CreateText($@"{tempFolder}\{SmartDesignStrings.MovementsFileName}.json"))
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, list);
            }

            return true;
        }

        public static bool ExportOsteotomyHandlerToTempFolder(List<string> requiredPartNames,
            CMFImplantDirector director, string tempFolder)
        {
            var objectManager = new CMFObjectManager(director);

            var list = new SmartDesignPartOsteotomyHandlerList();

            foreach (var partName in requiredPartNames)
            {
                if (partName == string.Empty)
                {
                    continue;
                }

                if (!ProPlanImportUtilities.IsOsteotomyPlane(partName))
                {
                    continue;
                }

                var rhinoObject = objectManager.GetAllBuildingBlockRhinoObjectByMatchingName(IBB.ProPlanImport, partName).FirstOrDefault();
                if (rhinoObject == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Could not find the part: {partName}");
                    continue;
                }

                var osteotomyHandler = new OsteotomyHandlerData();
                osteotomyHandler.DeSerialize(rhinoObject.Attributes.UserDictionary);

                var handlerDictionary = new Dictionary<string, double[]>();

                if (osteotomyHandler.HandlerIdentifier == null)
                {
                    continue;
                }

                for (var index = 0; index < osteotomyHandler.HandlerIdentifier.GetLength(0); index++)
                {
                    handlerDictionary.Add(osteotomyHandler.HandlerIdentifier[index],
                        osteotomyHandler.HandlerCoordinates.GetRow(index));
                }

                list.ExportedParts.Add(new SmartDesignPartOsteotomyHandler()
                {
                    OsteotomyPartName = partName,
                    OsteotomyThickness = osteotomyHandler.OsteotomyThickness,
                    OsteotomyType = osteotomyHandler.OsteotomyType,
                    OsteotomyHandler = handlerDictionary,
                });
            }

            if (list.ExportedParts.Count > 0)
            {
                using (var file = File.CreateText($@"{tempFolder}\{SmartDesignStrings.OsteotomyHandlerFileName}.json"))
                {
                    var serializer = new JsonSerializer();
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(file, list);
                }

                return true;
            }

            return false;
        }

        private static string PrepareSmartDesignCommand(ISmartDesignRecutModel dataModel, string tempFolder, string outputSuffix, string outputPath, bool hasOsteotomyHandler)
        {
            var command = "smartdesign ";
            switch (dataModel.RecutType)
            {
                case SmartDesignOperations.RecutLefort:
                    var lefortDataModel = dataModel as SmartDesignLefortRecutModel;

                    var isLefortWedgeOperation = lefortDataModel.WedgeOperation;
                    command += isLefortWedgeOperation ? "wedge-lefort " : "recut-lefort ";

                    command += $"--osteotomy \"{Path.Combine(tempFolder, lefortDataModel.Osteotomy)}.stl\" " +
                               $"--maxilla \"{Path.Combine(tempFolder, lefortDataModel.Maxilla)}.stl\" " +
                               $"--skull \"{Path.Combine(tempFolder, lefortDataModel.Skull)}.stl\" ";

                    if (lefortDataModel.PterygoidCuts.Any())
                    {
                        command += "--pterygoid-cuts ";

                        foreach (var pterygoidCut in lefortDataModel.PterygoidCuts)
                        {
                            command += $"\"{Path.Combine(tempFolder, pterygoidCut)}.stl\" ";
                        }
                    }

                    if (isLefortWedgeOperation)
                    {
                        command += $"--movements \"{Path.Combine(tempFolder, SmartDesignStrings.MovementsFileName)}.json\" ";

                        if (!string.IsNullOrEmpty(lefortDataModel.SkullComplete))
                        {
                            command += $"--skull-complete \"{Path.Combine(tempFolder, lefortDataModel.SkullComplete)}.stl\" ";
                        }

                        if (lefortDataModel.ExtendCut)
                        {
                            command += "--extend-cut ";
                        }
                    }

                    break;

                case SmartDesignOperations.RecutBSSO:
                    var bssoDataModel = dataModel as SmartDesignBSSORecutModel;
                        
                    command += bssoDataModel.WedgeOperation ? "wedge-bsso " : "recut-bsso ";

                    if (bssoDataModel.Osteotomies.Any())
                    {
                        command += "--osteotomy ";

                        foreach (var osteotomy in bssoDataModel.Osteotomies)
                        {
                            command += $"\"{Path.Combine(tempFolder, osteotomy)}.stl\" ";
                        }
                    }

                    command += $"--body \"{Path.Combine(tempFolder, bssoDataModel.Body)}.stl\" ";

                    command += bssoDataModel.RamusR.Any()
                        ? $"--ramus-r \"{Path.Combine(tempFolder, bssoDataModel.RamusR)}.stl\" "
                        : "";

                    command += bssoDataModel.RamusL.Any()
                        ? $"--ramus-l \"{Path.Combine(tempFolder, bssoDataModel.RamusL)}.stl\" "
                        : "";

                    if (bssoDataModel.WedgeOperation)
                    {
                        command += $"--movements \"{Path.Combine(tempFolder, SmartDesignStrings.MovementsFileName)}.json\" ";

                        if (!string.IsNullOrEmpty(bssoDataModel.MandibleComplete))
                        {
                            command += $"--mandible-complete \"{Path.Combine(tempFolder, bssoDataModel.MandibleComplete)}.stl\" ";
                        }

                        if (!string.IsNullOrEmpty(bssoDataModel.MandibleTeeth))
                        {
                            command += $"--teeth \"{Path.Combine(tempFolder, bssoDataModel.MandibleTeeth)}.stl\" ";
                        }

                        if (!string.IsNullOrEmpty(bssoDataModel.NerveR))
                        {
                            command += $"--nerve-r \"{Path.Combine(tempFolder, bssoDataModel.NerveR)}.stl\" ";
                        }

                        if (!string.IsNullOrEmpty(bssoDataModel.NerveL))
                        {
                            command += $"--nerve-l \"{Path.Combine(tempFolder, bssoDataModel.NerveL)}.stl\" ";
                        }
                    }
                    else if (bssoDataModel.AnteriorOnly)
                    {
                        command += "--anterior-only ";
                    }

                    if (bssoDataModel.SplitSso)
                    {
                        command += "--split-sso ";
                    }

                    break;

                case SmartDesignOperations.RecutGenio:
                    var genioDataModel = dataModel as SmartDesignGenioRecutModel;

                    var isGenioWedgeOperation = genioDataModel.WedgeOperation;
                    command += isGenioWedgeOperation ? "wedge-genio " : "recut-genio ";

                    command += $"--osteotomy \"{Path.Combine(tempFolder, genioDataModel.Osteotomy)}.stl\" " +
                               $"--body \"{Path.Combine(tempFolder, genioDataModel.Mandible)}.stl\" " +
                               $"--chin \"{Path.Combine(tempFolder, genioDataModel.Chin)}.stl\" ";

                    if (isGenioWedgeOperation)
                    {
                        command += $"--movements \"{Path.Combine(tempFolder, SmartDesignStrings.MovementsFileName)}.json\" ";

                        if (!string.IsNullOrEmpty(genioDataModel.MandibleComplete))
                        {
                            command += $"--mandible-complete \"{Path.Combine(tempFolder, genioDataModel.MandibleComplete)}.stl\" ";
                        }
                    }

                    break;

                case SmartDesignOperations.RecutSplitMax:
                    var splitMaxDataModel = dataModel as SmartDesignSplitMaxRecutModel;
                    command += "recut-splitmax ";

                    if (splitMaxDataModel.Osteotomies.Any())
                    {
                        command += "--osteotomies ";

                        foreach (var osteotomy in splitMaxDataModel.Osteotomies)
                        {
                            command += $"\"{Path.Combine(tempFolder, osteotomy)}.stl\" ";
                        }
                    }

                    if (splitMaxDataModel.MaxillaParts.Any())
                    {
                        command += "--maxilla-parts ";

                        foreach (var maxilla in splitMaxDataModel.MaxillaParts)
                        {
                            command += $"\"{Path.Combine(tempFolder, maxilla)}.stl\" ";
                        }
                    }

                    break;
            }

            if (hasOsteotomyHandler)
            {
                command += $"--handlers \"{Path.Combine(tempFolder, SmartDesignStrings.OsteotomyHandlerFileName)}.json\" ";
            }

            command += $"--suffix \"{outputSuffix}\"";

            return $"\"{command}\" \"{outputPath}\"";
        }

        public static List<string> ExecuteBatFileWithReturn(string scriptPath)
        {
            var results = new List<string>();
            var error = new List<string>();

            var process = new Process();
            {
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.FileName = scriptPath;

                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            results.Add(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            error.Add(e.Data);
                        }
                    };

                    process.Refresh();
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (process.WaitForExit(20000))
                    {
                        process.WaitForExit();

                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    else
                    {
                        process.Kill();
                    }

                    process.CancelOutputRead();
                    process.CancelErrorRead();
                }
            }

            return results;
        }

        public static bool ProcessSmartDesignOutput(ISmartDesignRecutModel dataModel, CMFImplantDirector director, string outputPath)
        {
            if (!PostProcessSmartDesignOutput(dataModel, outputPath))
            {
                return false;
            }

            if (!PrepareSmartDesignOutputAsImportRecutInput(director, outputPath))
            {
                return false;
            }

            return true;
        }

        private static bool PostProcessSmartDesignOutput(string outputPath, string osteotomyStlFilename)
        {
            var directory = new DirectoryInfo(outputPath);
            var stlFiles = directory.GetFiles(osteotomyStlFilename, SearchOption.TopDirectoryOnly);

            if (!stlFiles.Any())
            {
                return false;
            }

            var tempFolder = CreateTempFolder(SmartDesignStrings.WedgeFolderName, false);

            foreach (var stlFile in stlFiles)
            {
                if (stlFile.Name.ToLower().Contains("_bone"))
                {
                    File.Move(stlFile.FullName, $"{Path.Combine(tempFolder, stlFile.Name)}");
                }
            }

            return true;
        }

        private static bool PostProcessSmartDesignOutput(ISmartDesignRecutModel dataModel, string outputPath)
        {
            var processed = true;

            switch (dataModel.RecutType)
            {                
                case SmartDesignOperations.RecutBSSO:
                    var bssoDataModel = dataModel as SmartDesignBSSORecutModel;

                    if (bssoDataModel.WedgeOperation)
                    {
                        //for wedge-bsso, move wedge to another folder
                        bssoDataModel.Osteotomies.ForEach(osteotomy =>
                        {
                            processed &= PostProcessSmartDesignOutput(outputPath, $"{osteotomy}*.stl");
                        });
                    }

                    break;

                case SmartDesignOperations.RecutLefort:
                    var lefortDataModel = dataModel as SmartDesignLefortRecutModel;

                    if (lefortDataModel.WedgeOperation)
                    {
                        //for wedge-lefort, move wedge to another folder
                        processed = PostProcessSmartDesignOutput(outputPath, $"{lefortDataModel.Osteotomy}*.stl");
                    }

                    break;

                case SmartDesignOperations.RecutGenio:
                    var genioDataModel = dataModel as SmartDesignGenioRecutModel;

                    if (genioDataModel.WedgeOperation)
                    {
                        //for wedge-genio, move wedge to another folder
                        processed = PostProcessSmartDesignOutput(outputPath, $"{genioDataModel.Osteotomy}*.stl");
                    }

                    break;
                default:
                    break;
            }

            return processed;
        }

        public static bool FindPlannedTeethAndNerves(ISmartDesignRecutModel dataModel, CMFImplantDirector director, out List<string> partsToExcludeRegistration)
        {
            var foundParts = false;
            partsToExcludeRegistration = new List<string>();

            switch (dataModel.RecutType)
            {
                case SmartDesignOperations.RecutBSSO:

                    if (!dataModel.WedgeOperation)
                    {
                        break;
                    }

                    var bssoDataModel = dataModel as SmartDesignBSSORecutModel;

                    if (!string.IsNullOrEmpty(bssoDataModel.MandibleTeeth))
                    {
                        partsToExcludeRegistration.Add(GetPlannedPartName(director, bssoDataModel.MandibleTeeth));
                        foundParts = true;
                    }

                    if (!string.IsNullOrEmpty(bssoDataModel.NerveL))
                    {
                        partsToExcludeRegistration.Add(GetPlannedPartName(director, bssoDataModel.NerveL));
                        foundParts = true;
                    }

                    if (!string.IsNullOrEmpty(bssoDataModel.NerveR))
                    {
                        partsToExcludeRegistration.Add(GetPlannedPartName(director, bssoDataModel.NerveR));
                        foundParts = true;
                    }

                    break;
            }

            return foundParts;
        }

        private static string GetPlannedPartName(CMFImplantDirector director, string partName)
        {
            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();

            var partNameWithoutSurgeryStage = ProPlanPartsUtilitiesV2.GetPartNameWithoutSurgeryStage(partName);
            var rhinoObject = objectManager.GetAllBuildingBlockRhinoObjectByMatchingName(IBB.ProPlanImport, $"0[2-9]{partNameWithoutSurgeryStage}$").FirstOrDefault();

            // If no planned part is found, SmartDesign will automatically rename it with "05" prefix
            var plannedPartName = rhinoObject != null
                ? proPlanImportComponent.GetPartName(rhinoObject.Name)
                : $"05{partNameWithoutSurgeryStage}";

            return plannedPartName;
        }

        private static bool PrepareSmartDesignOutputAsImportRecutInput(CMFImplantDirector director, string outputPath)
        {
            var hasInputForImportRecut = false;

            var directory = new DirectoryInfo(outputPath);
            var stlFiles = directory.GetFiles("*.stl", SearchOption.TopDirectoryOnly);

            var proPlanImportComponent = new ProPlanImportComponent();
            var objectManager = new CMFObjectManager(director);

            foreach (var stlFile in stlFiles)
            {
                //compare with mesh in doc
                var partName = Path.GetFileNameWithoutExtension(stlFile.FullName);
                var block = proPlanImportComponent.GetProPlanImportBuildingBlock(partName);

                if (!objectManager.HasBuildingBlock(block))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Cannot find {partName}!");
                    File.Delete(stlFile.FullName);
                    continue;
                }

                hasInputForImportRecut = true;
            }

            return hasInputForImportRecut;
        }

        public static bool AddWedgesAsReferenceEntities(ISmartDesignRecutModel dataModel, CMFImplantDirector director)
        {
            var success = true;

            switch (dataModel.RecutType)
            {
                case SmartDesignOperations.RecutBSSO:
                case SmartDesignOperations.RecutLefort:
                case SmartDesignOperations.RecutGenio:
                    if (dataModel.WedgeOperation)
                    {
                        var path = Path.Combine(Path.GetTempPath(), SmartDesignStrings.WedgeFolderName);
                        var directory = new DirectoryInfo(path);
                        var stlFiles = directory.GetFiles("*.stl", SearchOption.TopDirectoryOnly);
                        
                        if (!stlFiles.Any())
                        {
                            success = false;
                            break;
                        }

                        var mergedWedges = new IDSMesh();

                        foreach (var stlFile in stlFiles)
                        {
                            //merge into one STL and add as reference entity with name: "Wedge_BSSO"
                            var read = StlUtilitiesV2.StlBinaryToIDSMesh(stlFile.FullName, out var mesh);
                            if (!read)
                            {
                                success = false;
                                break;
                            }

                            mergedWedges.Append(mesh);
                        }

                        if (success)
                        {
                            var objectManager = new CMFObjectManager(director);
                            var cloneReferenceBuildingBlock = BuildingBlocks.Blocks[IBB.ReferenceEntities].Clone();
                            string layerName;
                            switch (dataModel.RecutType)
                            {
                                case SmartDesignOperations.RecutBSSO:
                                    layerName = SmartDesignStrings.WedgeBSSOLayerName;
                                    break;
                                case SmartDesignOperations.RecutLefort:
                                    layerName = SmartDesignStrings.WedgeLefortLayerName;
                                    break;
                                case SmartDesignOperations.RecutGenio:
                                    layerName = SmartDesignStrings.WedgeGenioLayerName;
                                    break;
                                default:
                                    throw new IDSException("Recut type not found");
                            }

                            cloneReferenceBuildingBlock.Layer = string.Format(cloneReferenceBuildingBlock.Layer, layerName);
                            var newGuid = objectManager.AddNewBuildingBlock(cloneReferenceBuildingBlock, RhinoMeshConverter.ToRhinoMesh(mergedWedges));

                            success = newGuid != Guid.Empty;

                            SystemTools.DeleteRecursively(path);
                        }
                    }

                    break;
                default:
                    break;
            }

            return success;
        }
    }
}