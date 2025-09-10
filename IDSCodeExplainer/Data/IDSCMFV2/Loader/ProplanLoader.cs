using IDS.CMF.V2.ExternalTools;
using IDS.CMF.V2.FileSystem;
using IDS.CMF.V2.Logics;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Geometry;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Loader;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IDS.CMF.V2.Loader
{
    public class ProplanLoader : IPreopLoader
    {
        private readonly IConsole _console;
        private readonly string _filePath;
        private string _tempDirectory;
        private List<IPreopLoadResult> _preLoadOutput;

        public ProplanLoader(IConsole console, string filePath)
        {
            _console = console;
            _filePath = filePath;
            _preLoadOutput = new List<IPreopLoadResult>();
        }

        public List<IPreopLoadResult> PreLoadPreop()
        {
            var outputProplan = new List<IPreopLoadResult>();
            _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDirectory);

            var cmdArgs = $"\"{_filePath}\" \"{_tempDirectory}\"";

            if (!RunLoadProplanx86Executable(cmdArgs))
            {
                return null;
            }

            var directoryInfo = new DirectoryInfo(_tempDirectory);
            if (directoryInfo.GetFiles("*.stl").Length == 0)
            {
                _console.WriteErrorLine("No output meshes generated.");
                return null;
            }

            if (directoryInfo.GetFiles("*.json").Length == 0)
            {
                _console.WriteErrorLine("No json file generated.");
                return null;
            }

            var coordinateSystems = LoadCoordinateSystems(directoryInfo);

            var files = directoryInfo.GetFiles("*.stl").Select(file =>
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);

                return new
                {
                    FullName = file.FullName,
                    FileNameWithoutExtension = fileNameWithoutExtension,
                    PartName = fileNameWithoutExtension.Trim()
                };
            });

            var proPlanImportComponent = new ProPlanImportComponentV2();
            var filteredPartNames = proPlanImportComponent.GetRequiredPartNames(files.Select(file => file.PartName));
            var filteredFiles = files.Where(file => filteredPartNames.Contains(file.PartName));

            foreach (var file in filteredFiles)
            {
                var meshName = file.FileNameWithoutExtension;
                outputProplan.Add(new ProplanLoadResult(meshName.Trim(), file.FullName, GetTransform(coordinateSystems, meshName)));
            }

            _preLoadOutput = outputProplan.ToList();

            return _preLoadOutput;
        }

        public void CleanUp()
        {
            if (!string.IsNullOrEmpty(_tempDirectory) && Directory.Exists(_tempDirectory))
            {
                var directoryInfo = new DirectoryInfo(_tempDirectory);
                directoryInfo.Delete(true);
            }
        }

        public List<IPreopLoadResult> ImportPreop()
        {
            if (_preLoadOutput == null || !_preLoadOutput.Any())
            {
                _console.WriteErrorLine("Input provided is invalid!");
                return null;
            }

            var outputProplan = new List<IPreopLoadResult>();

            var errorDuringRead = false;
            var fileWithError = string.Empty;
            Parallel.ForEach(_preLoadOutput, (tuple, state) =>
            {
                IMesh currentStl;
                var read = StlUtilitiesV2.StlBinaryToIDSMesh(tuple.FilePath, out currentStl);
                if (!read)
                {
                    errorDuringRead = true;
                    fileWithError = tuple.Name;
                    state.Break();
                }

                outputProplan.Add(new ProplanLoadResult(tuple, currentStl));
            });

            if (errorDuringRead)
            {
                _console.WriteErrorLine("Something went wrong while reading the STL file: {0}", fileWithError);
                return null;
            }

            CleanUp();
            return outputProplan;
        }

        public bool GetPlanes(out IPlane sagittalPlane, out IPlane axialPlane, out IPlane coronalPlane, out IPlane midSagittalPlane)
        {
            sagittalPlane = IDSPlane.Unset;
            axialPlane = IDSPlane.Unset;
            coronalPlane = IDSPlane.Unset;
            midSagittalPlane = IDSPlane.Unset;

            var proPlanPlanesExtractor = new ProPlanPlanesExtractor(_console);
            if (!proPlanPlanesExtractor.GetPlanesFromSppc(_filePath))
            {
                return false;
            }

            sagittalPlane = proPlanPlanesExtractor.SagittalPlane;
            axialPlane = proPlanPlanesExtractor.AxialPlane;
            coronalPlane = proPlanPlanesExtractor.CoronalPlane;
            midSagittalPlane = proPlanPlanesExtractor.MidSagittalPlane;

            return true;
        }

        private bool RunLoadProplanx86Executable(string cmdArgs)
        {
            var res = new CMFResourcesV2();

#if INTERNAL
            _console.WriteLine($"[IDS::INTERNAL] MatSdkConsole arguments: {cmdArgs}");
#endif

            if (ExternalToolsUtilities.RunExternalTool(res.LoadProplanMatSDKConsole, cmdArgs, string.Empty, false, _console))
            {
                return true;
            }
            //filesCreated.ForEach(f => { if (File.Exists(f)) { File.Delete(f); } });
            return false;
        }

        private List<CoordinateSystemJson> LoadCoordinateSystems(DirectoryInfo directory)
        {
            var res = new CMFResourcesV2();
            var coordinateSystemJson = directory.GetFiles(res.ProPlanImportCoordinateSystemFileName).First().FullName;
            var parser = new CoordinateSystemJsonParser();
            var coordinateSystems = parser.LoadCoordinateSystems(coordinateSystemJson);
            return coordinateSystems;
        }

        private static IDSTransform GetTransform(List<CoordinateSystemJson> coordinateSystems, string meshName)
        {
            if (coordinateSystems.All(cs => cs.Part != meshName))
            {
                return IDSTransform.Identity;
            }

            var matrix = coordinateSystems.Last(cs => cs.Part == meshName).Matrix;
            return ParserUtilities.GetTransform(matrix);
        }

        public List<Tuple<string, bool>> GetPartInfos()
        {
            return ProPlanPartsUtilitiesV2.GetMatchingStlNamesWithProPlanImportJsonFromSppc(_filePath, _console).Select(name => new Tuple<string, bool>(name, false)).ToList();
        }

        public bool ExportPreopToStl(List<string> partNames, string outputDirectory)
        {
            if (string.IsNullOrEmpty(_tempDirectory) || !Directory.Exists(_tempDirectory))
            {
                throw new Exception("PreLoadPreop not performed!");
            }

            foreach (var partName in partNames)
            {
                File.Copy(Path.Combine(_tempDirectory, $"{partName}.stl"), Path.Combine(outputDirectory, $"{partName}.stl"));
            }

            return true;
        }

        public bool GetOsteotomyHandler(out List<IOsteotomyHandler> osteotomyHandler)
        {
            osteotomyHandler = new List<IOsteotomyHandler>();
            return false;
        }
    }
}
