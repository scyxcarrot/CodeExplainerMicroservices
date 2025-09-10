using IDS.Core.Fea;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.V2.Utilities;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace IDS.Core.Utilities
{
    /// <summary>
    /// Class providing interoperability with the TetGen tool.
    /// </summary>
    public class ExternalToolInterop
    {
        /// <summary>
        /// Calculixes the execute.
        /// </summary>
        /// <param name="inpFilePath">The inp file path.</param>
        /// <exception cref="System.IO.FileNotFoundException">INP file does not exist.</exception>
        public static bool CalculixExecute(string inpFilePath)
        {
            // Should not be executed on non-existing file
            if (!File.Exists(inpFilePath))
            {
                throw new FileNotFoundException("INP file does not exist.", inpFilePath);
            }

            // Folder and filename without extension
            string inpFileFolder = Path.GetDirectoryName(inpFilePath);
            string inpSimulation = Path.GetFileNameWithoutExtension(inpFilePath);

            // Run
            var resources = new Resources();
            string commandArguments = string.Format(CultureInfo.InvariantCulture, @"""{0}""", inpSimulation);
            bool succesfullyExecuted = RunExternalTool(resources.CalculixExecutable, commandArguments, inpFileFolder, true);

            if(succesfullyExecuted)
            {
                // Remove unnecessary outputs
                File.Delete(Path.Combine(inpFileFolder, string.Format("{0}.sta", inpSimulation)));
                File.Delete(Path.Combine(inpFileFolder, string.Format("{0}.dat", inpSimulation)));
                File.Delete(Path.Combine(inpFileFolder, string.Format("{0}.cvg", inpSimulation)));
                File.Delete(Path.Combine(inpFileFolder, "spooles.out"));
            }

            return succesfullyExecuted;
        }

        public static Mesh SmoothImplantEdges(Mesh flangesTop, Mesh flangesSide, Mesh flangesBottom, 
            double edgeRadiusTop, double edgeRadiusBottom, double topMinEdgeLength, double topMaxEdgeLength,
            double bottomMinEdgeLength, double bottomMaxEdgeLength)
        {
            var flangesTopStlPath = StlUtilities.WriteStlTempFile(flangesTop);
            var flangesSideStlPath = StlUtilities.WriteStlTempFile(flangesSide);
            var flangesBottomStlPath = StlUtilities.WriteStlTempFile(flangesBottom);
            var roundedStlTargetPath = System.IO.Path.GetTempPath() + "IDS_" + Guid.NewGuid().ToString() + ".stl";

            var filesCreated = new List<string> { flangesTopStlPath, flangesSideStlPath, flangesBottomStlPath, roundedStlTargetPath };

            var cmdArgs = $"SmoothEdge {flangesTopStlPath} {flangesSideStlPath} {flangesBottomStlPath} {roundedStlTargetPath}" +
                           $" {edgeRadiusTop} {edgeRadiusBottom} {topMinEdgeLength} {topMaxEdgeLength} {bottomMinEdgeLength} {bottomMaxEdgeLength} {10}";

            if (!ExternalToolsUtilities.RunMatSdkConsolex86Executable(cmdArgs, filesCreated, new IDSRhinoConsole(), true))
            {
                return null;
            }

            Mesh flangesRounded;
            StlUtilities.StlBinary2RhinoMesh(roundedStlTargetPath, out flangesRounded);
            flangesRounded.Vertices.CombineIdentical(true, true);
            filesCreated.ForEach(f => { if (File.Exists(f)) { File.Delete(f); } });

            return flangesRounded;
        }

        [Obsolete("Obsolete, please use MeshUtilitiesV2.PerformSmoothing")]
        public static Mesh PerformSmoothing(Mesh mesh, bool useCompensation, bool preserveBadEdges, bool preserveSharpEdges, double sharpEdgeAngle, double smoothenFactor, int iterations)
        {
            var meshStlPath = StlUtilities.WriteStlTempFile(mesh);
            var smoothenStlTargetPath = Path.GetTempPath() + "IDS_" + Guid.NewGuid() + ".stl";

            var filesCreated = new List<string> { meshStlPath, smoothenStlTargetPath };

            var cmdArgs = $"Smooth {meshStlPath} {smoothenStlTargetPath} FIRST_ORDER_LAPLACIAN {useCompensation} {preserveBadEdges} {preserveSharpEdges} {sharpEdgeAngle} {smoothenFactor} {iterations}";
            
            if (!ExternalToolsUtilities.RunMatSdkConsolex86Executable(cmdArgs, filesCreated, new IDSRhinoConsole(), true))
            {
                return null;
            }

            Mesh smoothen;
            StlUtilities.StlBinary2RhinoMesh(smoothenStlTargetPath, out smoothen);
            smoothen.Vertices.CombineIdentical(true, true);
            filesCreated.ForEach(f => { if (File.Exists(f)) { File.Delete(f); } });

            return smoothen;
        }

        public static Mesh PerformQualityPreservingReduceTriangles(Mesh mesh, double qualityThreshold, double maximalGeometricError, bool checkMaximalEdgeLength, double maximalEdgeLength, 
            int numberOfIterations, bool skipBadEdges, bool preserveSurfaceBorders, int operationCount = 1, bool enableLogging = true)
        {
            var meshStlPath = StlUtilities.WriteStlTempFile(mesh);
            var remeshedStlTargetPath = Path.GetTempPath() + "IDS_" + Guid.NewGuid() + ".stl";

            var filesCreated = new List<string> { meshStlPath, remeshedStlTargetPath };

            var cmdArgs = $"QualityPreservingReduceTriangles {meshStlPath} {remeshedStlTargetPath} {qualityThreshold} {maximalGeometricError} {checkMaximalEdgeLength} {maximalEdgeLength}" +
                $" {numberOfIterations} {skipBadEdges} {preserveSurfaceBorders} {operationCount}";

            if (!ExternalToolsUtilities.RunMatSdkConsolex86Executable(cmdArgs, filesCreated, new IDSRhinoConsole(), true))
            {
                return null;
            }

            Mesh remeshed;
            StlUtilities.StlBinary2RhinoMesh(remeshedStlTargetPath, out remeshed);
            remeshed.Vertices.CombineIdentical(true, true);
            filesCreated.ForEach(f => { if (File.Exists(f)) { File.Delete(f); } });

            return remeshed;
        }

        /// <summary>
        /// Remesh a Rhino Mesh using ACVD remesher.
        /// </summary>
        /// <param name="inmesh">The inmesh.</param>
        /// <param name="remeshed">The remeshed.</param>
        /// <param name="TargetEdgeLength">Length of the target edge.</param>
        /// <param name="Gradation">The gradation.</param>
        /// <param name="subdivision">The subdivision.</param>
        /// <param name="ForceManifold">if set to <c>true</c> [force manifold].</param>
        /// <returns></returns>
        public static bool AcvdRemesh(Mesh inmesh, out Mesh remeshed, double TargetEdgeLength, int Gradation = 0, double subdivision = 1.0 / 4.0, bool ForceManifold = false)
        {
            // Prepare the mesh
            string input_file = StlUtilities.WriteStlTempFile(inmesh);
            string output_dir = System.IO.Path.GetTempPath();
            output_dir = output_dir.Trim(Path.DirectorySeparatorChar); // Necessary for ACVD.exe
            string output_prefix = "IDS_" + Guid.NewGuid().ToString();
            string output_name = output_prefix + ".stl";
            string output_file = Path.Combine(output_dir, output_name);

            // Calculate some ACVD parameters
            double targetArea = Math.Sqrt(3.0) / 4.0 * Math.Pow(TargetEdgeLength,2);
            AreaMassProperties am = AreaMassProperties.Compute(inmesh);
            double MeshArea = am.Area;
            int nF = (int)(MeshArea / targetArea + 1.0); // ceil
            int nV = (int)(nF / 2.0 + 1.0); // ceil

            // Calculate subdivision factor split edges longer than decF * TargetEdgeLength
            double MeanEdgeLength = MeshUtilities.GetMeanEdgeLength(inmesh);

            // After the initial subdivision phase, you want an edge length that is smaller than your
            // target edge length
            double splitThresholdLength = subdivision * TargetEdgeLength / MeanEdgeLength;

            // Set up the command
            Resources resources = new Resources();
            string cmd_args = string.Format(CultureInfo.InvariantCulture, "\"{0}\" {1} {2} -l {3,0:F5} -d 0 -o \"{4}\" -of \"{5}\"", input_file, nV, Gradation, splitThresholdLength, output_dir, output_name);
            if (ForceManifold)
                cmd_args = cmd_args + " -m 1 -sf 2";

            // Execute
            bool executedSuccessfully = RunExternalTool(resources.AcvdExecutable, cmd_args, string.Empty, false);
            if (!executedSuccessfully)
            {
                remeshed = null;
                return false;
            }

            // Read results
            bool read_ok = StlUtilities.StlBinary2RhinoMesh(output_file, out remeshed);
            remeshed.Vertices.CombineIdentical(true, true);
            return read_ok;
        }

        /// <summary>
        /// Volume mesh using TetGen
        /// </summary>
        /// <param name="meshPath">The mesh path.</param>
        /// <param name="edgeLength">Length of the edge.</param>
        /// <param name="inpPath">The inp path.</param>
        /// <param name="qmax">The qmax.</param>
        /// <param name="volmax">The maximum volume.</param>
        /// <returns></returns>
        public static InpPart TetGenVolumeMesh(string meshPath, double edgeLength, double qmax, double volmax)
        {
            // Run TetGen
            TetGenExecute(meshPath, edgeLength, qmax, volmax);

            // Determine output file paths
            string meshFolder = Path.GetDirectoryName(meshPath);
            string meshName = Path.GetFileNameWithoutExtension(meshPath);
            string nodeFile = Path.Combine(meshFolder, meshName + ".1.node");
            string eleFile = Path.Combine(meshFolder, meshName + ".1.ele");
            string faceFile = Path.Combine(meshFolder, meshName + ".1.face");
            string edgeFile = Path.Combine(meshFolder, meshName + ".1.edge");
            string smeshFile = Path.Combine(meshFolder, meshName + ".1.smesh");

            // Convert output to Inp file
            InpPart inp = TetGenConvertToInp(nodeFile, eleFile);

            // Delete the tetgen output
            File.Delete(nodeFile);
            File.Delete(eleFile);
            File.Delete(faceFile);
            File.Delete(edgeFile);
            File.Delete(smeshFile);

            // success
            return inp;
        }

        /// <summary>
        /// Tets the gen execute.
        /// </summary>
        /// <param name="meshPath">The mesh path.</param>
        /// <param name="qmax">The qmax.</param>
        /// <param name="volmax">The volmax.</param>
        /// <exception cref="System.Exception">Something went wrong while creating the TetMesh</exception>
        private static void TetGenExecute(string meshPath, double edgeLength, double qmax, double inputVolmax)
        {
            var volmax = inputVolmax;
            // Default value for maximum volume
            if (volmax <= 0.0)
            {
                volmax = Math.Pow(edgeLength * 1.2, 3) / (6 * Math.Sqrt(2));
            }

            // Set up the command
            var resources = new Resources();
            string tetgenExecutable = resources.TetgenExecutable;
            string commandArguments = string.Format(CultureInfo.InvariantCulture, "\"{0}\" -Y -q{1,0:F5} -a{2,0:F5}", meshPath, qmax, volmax);

            // Call the command: new process
            bool succesfullyExecuted = RunExternalTool(tetgenExecutable, commandArguments, string.Empty, false);
            if (!succesfullyExecuted)
            {
                throw new Exception("Something went wrong while creating the TetMesh");
            }
        }

        /// <summary>
        /// Converts the tet gen files to inp file.
        /// </summary>
        /// <param name="partName">Name of the part.</param>
        /// <param name="nodeFile">The node file.</param>
        /// <param name="eleFile">The ele file.</param>
        /// <param name="inpFile">The inp file.</param>
        /// <returns></returns>
        private static InpPart TetGenConvertToInp(string nodeFile, string eleFile)
        {
            InpPart part = new InpPart();
            part.Nodes = ReadIndexedArrayFile<double>(nodeFile, 3);
            part.Elements = ReadIndexedArrayFile<int>(eleFile, 4);

            return part;
        }

        /// <summary>
        /// Reads the indexed array file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="indexedArrayFile">The indexed array file.</param>
        /// <param name="valuesPerLine">The values per line.</param>
        /// <returns></returns>
        private static List<T[]> ReadIndexedArrayFile<T>(string indexedArrayFile, int valuesPerLine)
        {
            char[] delimiters = new char[] { ' ' };
            string[] parts;
            string line;
            List<T[]> values = new List<T[]>();

            // Read Line by line
            System.IO.StreamReader fileReader = new System.IO.StreamReader(indexedArrayFile);
            // First line does not contain elem data
            line = fileReader.ReadLine();
            // Parse until end
            while ((line = fileReader.ReadLine()) != null)
            {
                parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == valuesPerLine + 1)
                {
                    values.Add(parts.SubArray(1, valuesPerLine).ToNumeric<T>());
                }
            }
            fileReader.Close();

            return values;
        }

        [Obsolete("Please use the new method in IDSCoreV2's ExternalToolsUtilities")]
        public static bool RunExternalTool(string executablePath, string commandArguments, string workingDirectory,
            bool useShellExecute, bool enableLogging = true)
        {
            return ExternalToolsUtilities.RunExternalTool(executablePath, commandArguments, workingDirectory, useShellExecute, new IDSRhinoConsole(), enableLogging);
        }

        public static bool SpawnRunExternalTool(string executablePath, string commandArguments, string workingDirectory, Func<Process, bool> procWaitFunc, bool enableLogging = true)
        {
            if (enableLogging)
            {
                IDSPluginHelper.WriteLine(Enumerators.LogCategory.Diagnostic, "Executing {0} {1}", executablePath,
                    commandArguments);
                IDSPluginHelper.WriteLine(Enumerators.LogCategory.Diagnostic, "Working directory {0}",
                    workingDirectory);
            }

            // Call the command: new process
            var startInfo = new ProcessStartInfo()
            {
                FileName = $"\"{executablePath}\"",
                Arguments = commandArguments,
                UseShellExecute = true,
                RedirectStandardOutput = false,
                CreateNoWindow = false,
                WorkingDirectory = workingDirectory // directory to start proc.
            };

            using (var process = Process.Start(startInfo))
            {
                if (process != null)
                {
                    // procWaitFunc is not necessary, If it is null, just return true
                    return procWaitFunc == null || procWaitFunc(process);
                }
            }

            return false;
        }

        /// <summary>
        /// Execute a CPython script
        /// </summary>
        /// <param name="cPythonCommand"></param>
        /// <returns></returns>
        public static bool RunCPythonScript(string cPythonCommand)
        {
            string arguments = cPythonCommand.Replace(@"\", "/");
            Resources resources = new Resources();

            bool success = RunExternalTool(resources.CPythonExecutable, arguments, string.Empty, false);

            return success;
        }

        /// <summary>
        /// Compile the Python script and execute it once in the script context to make all functions available.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="scriptfile"></param>
        /// <returns>The script context</returns>
        public static Rhino.Runtime.PythonScript LoadIronPythonScript(RhinoDoc doc, string scriptfile)
        {
            Resources resources = new Resources();

            // Get the Python script path
#if DEBUG
            RhinoApp.WriteLine(string.Format("[IDS::DEBUG] Loading script from Python root dir {0}", resources.IronPythonScriptsFolder));
#endif
            if (null == resources.IronPythonScriptsFolder)
            {
                throw new IDSOperationFailed("Could not find the plugin directory!");
            }

            string scriptpath = System.IO.Path.Combine(resources.IronPythonScriptsFolder, scriptfile);
#if DEBUG
            RhinoApp.WriteLine(string.Format("[IDS::DEBUG] Loading script with path {0}", scriptpath));
#endif
            if (!System.IO.File.Exists(scriptpath))
            {
                throw new IDSOperationFailed("File not found: " + scriptpath);
            }

            // Create the script context/scope and execute the python code in it
            Rhino.Runtime.PythonScript scriptcontext;
            string script_text = resources.GeneratePythonOsEnvironmentSetUpScript() + System.IO.File.ReadAllText(scriptpath);
            scriptcontext = Rhino.Runtime.PythonScript.Create();
            scriptcontext.ScriptContextDoc = doc;
            try
            {
                Rhino.Runtime.PythonCompiledCode script_bin = scriptcontext.Compile(script_text);
                script_bin.Execute(scriptcontext);
            }
            catch (Exception exc)
            {
                RhinoApp.WriteLine("Exception thrown during execution of python script: \n" + exc.ToString());
                throw;
            }
            return scriptcontext;
        }
    }
}
