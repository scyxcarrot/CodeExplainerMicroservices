using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.TestLib;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using System.IO;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)
    [System.Runtime.InteropServices.Guid("1EF72365-0DE3-4AAD-AE6D-CC5D88E7BC43")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.ProPlanImport)]
    public class CMF_TestDisplayLayersAndParts : CmfCommandBase
    {
        public CMF_TestDisplayLayersAndParts()
        {
            Instance = this;
        }

        public static CMF_TestDisplayLayersAndParts Instance { get; private set; }

        public override string EnglishName => "CMF_TestDisplayLayersAndParts";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            //write to json
            var directory = DirectoryStructure.GetWorkingDir(doc);
            using (var file = File.CreateText($"{directory}\\LayersAndParts.json"))
            {
                file.Write(CMFImplantDirectorConverter.DumpAllLayerAndParts(director));
            }
            SystemTools.OpenExplorerInFolder(directory);

            return Result.Success;
        }
    }

#endif
}
