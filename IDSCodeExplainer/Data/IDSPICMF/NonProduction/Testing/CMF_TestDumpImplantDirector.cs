using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.TestLib;
using Rhino;
using Rhino.Commands;
using System.IO;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)
    [System.Runtime.InteropServices.Guid("0F827F31-A100-4BA6-903B-21A53E297C05")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestDumpImplantDirector : CmfCommandBase
    {
        public CMF_TestDumpImplantDirector()
        {
            Instance = this;
        }

        public static CMF_TestDumpImplantDirector Instance { get; private set; }

        public override string EnglishName => "CMF_TestDumpImplantDirector";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var workingDir = Path.Combine(DirectoryStructure.GetWorkingDir(director.Document), "Config");
            if (Directory.Exists(workingDir))
            {
                Directory.Delete(workingDir, true);
            }

            Directory.CreateDirectory(workingDir);

            var path = $"{workingDir}\\LayersAndParts.json";
            using (var streamWriter = File.CreateText(path))
            {
                var directorJson = CMFImplantDirectorConverter.DumpAllLayerAndParts(director);
                streamWriter.Write(directorJson);
            }

            path = $"{workingDir}\\config.json";
            using (var streamWriter = File.CreateText(path))
            {
                var directorJson = CMFImplantDirectorConverter.Dump(director, workingDir);
                streamWriter.Write(directorJson);
            }

            return Result.Success;
        }
    }
#endif
}
