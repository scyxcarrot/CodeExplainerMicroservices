using IDS.Amace.Enumerators;
using IDS.Amace.FileSystem;
using IDS.Amace.Relations;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using Rhino;
using Rhino.Commands;
using Rhino.FileIO;
using System.Collections.Generic;
using System.IO;

namespace IDS.Amace.Commands
{
    /**
     * Command to create a work file
     */

    [
     System.Runtime.InteropServices.Guid("8C6B5A13-F265-4128-A88E-9412C20D0115"),
     CommandStyle(Style.ScriptRunner),
     IDSCommandAttributes(true, DesignPhase.Draft)
    ]
    public class StartNewDraft : CommandBase<ImplantDirector>
    {
        public StartNewDraft()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static StartNewDraft TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "OpenWorkFile";

        /**
         * Load the MBV volume from an existing mesh in the document
         * instead of creating it.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Create work folder
            string workingDir;
            var success = DirectoryStructure.CreateWorkingDir(director.Document,
                new List<string>() { "inputs", "extrainputs", "extra_inputs" },
                new List<string>() { Path.GetFileName(director.Document.Path) },
                new List<string>() { "3dm", "mat" }, out workingDir);
            if (!success)
            {
                return Result.Failure;
            }

            // Set appropriate phase
            switch (director.documentType)
            {
                case DocumentType.CupQC:
                    {
                        director.draft++;
                        PhaseChanger.ChangePhase(director, DesignPhase.CupQC, false);
                        break;
                    }
                case DocumentType.ImplantQC:
                    {
                        director.draft++;
                        PhaseChanger.ChangePhase(director, DesignPhase.ImplantQC, false);
                        break;
                    }
                case DocumentType.Export:
                    {
                        director.draft = 1;
                        director.version++;
                        PhaseChanger.ChangePhase(director, DesignPhase.ImplantQC, false);
                        break;
                    }
                default:
                    {
                        return Result.Failure;
                    }
            }

            director.documentType = DocumentType.Work;
            director.InputFiles = new List<string> { director.Document.Path };

            // Create work file
            var workFileName =
                $"{workingDir}\\{director.Inspector.CaseId}_work_v{director.version:D}_draft{director.draft:D}.3dm";
            var opts = new FileWriteOptions();
            director.UpdateComponentVersions();
            director.Document.WriteFile(workFileName, opts);
            File.SetAttributes(workFileName, FileAttributes.Normal);

            // Discard changes and open the work file ('No' to suppress 'Save Changes')
            var command = "-_Open No \"" + workFileName + "\"";
            RhinoApp.RunScript(command, false);

            return Result.Success;
        }
    }
}