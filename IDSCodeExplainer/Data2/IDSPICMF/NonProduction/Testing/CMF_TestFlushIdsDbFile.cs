#if (STAGING)
using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using System.IO;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("924726FA-BE35-43BB-8A56-566A34241F5D")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_TestFlushIdsDbFile : CmfCommandBase
    {
        public CMF_TestFlushIdsDbFile()
        {
            Instance = this;
        }

        public static CMF_TestFlushIdsDbFile Instance { get; private set; }

        public override string EnglishName => "CMF_TestFlushIdsDbFile";

        /// <summary>
        /// This command is used to see the actual database that is stored inside the ids file
        /// </summary>
        /// <param name="doc">Rhino document instance</param>
        /// <param name="mode">Scripted or Interactive mode, if its pressed by user, then its Interactive</param>
        /// <param name="director">Our own custom class which holds the instance to the database</param>
        /// <returns></returns>
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var workingDirectory = DirectoryStructure.GetWorkingDir(director.Document);
            var databasePath = Path.Combine(workingDirectory, $"{director.caseId}.db");

            using (var fs = new FileStream(databasePath, FileMode.Create, FileAccess.Write))
            {
                var treeDatabaseByteArray = director.WriteIdsDatabaseToByteArray();
                fs.Write(treeDatabaseByteArray, 0, treeDatabaseByteArray.Length);

                IDSPluginHelper.WriteLine(LogCategory.Default, 
                    $"Copy of database exported to = {databasePath}");
                return Result.Success;
            }
        }
    }
}

#endif