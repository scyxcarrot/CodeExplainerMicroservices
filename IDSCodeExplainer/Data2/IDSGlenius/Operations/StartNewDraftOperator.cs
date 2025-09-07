using IDS.Core.Enumerators;
using IDS.Glenius.Enumerators;
using IDS.Glenius.FileSystem;
using IDS.Glenius.Relations;
using Rhino;
using System.Collections.Generic;
using System.IO;

namespace IDS.Glenius.Operations
{
    public class StartNewDraftOperator
    {
        public bool Execute(RhinoDoc doc, GleniusImplantDirector director)
        {
            // Set appropriate phase
            if (director.documentType == DocumentType.ScrewQC)
            {
                director.draft++;
                PhaseChanger.ChangePhase(director, DesignPhase.ScrewQC, false);
            }
            else if (director.documentType == DocumentType.ScaffoldQC)
            {
                director.draft++;
                PhaseChanger.ChangePhase(director, DesignPhase.ScaffoldQC, false);
            }
            else if (director.documentType == DocumentType.ApprovedQC)
            {
                director.version++;
                director.draft = 1;
                PhaseChanger.ChangePhase(director, DesignPhase.ScaffoldQC, false);
            }
            else
            {
                return false;
            }

            director.documentType = DocumentType.Work;
            director.InputFiles = new List<string> { director.Document.Path };

            string workingDirPath;
            if (DirectoryStructure.CreateWorkingDir(doc, new List<string>() {}, new List<string>() { Path.GetFileName(doc.Path) }, new List<string>() { "3dm" }, out workingDirPath))
            {
                var options = new Rhino.FileIO.FileWriteOptions();
                var workFileName = Path.Combine(workingDirPath, $"{director.caseId}_work_v{director.version:D}_d{director.draft:D}.3dm");
                if (director.Document.WriteFile(workFileName, options))
                {
                    File.SetAttributes(workFileName, FileAttributes.Normal);
                    var command = "-_Open \"" + workFileName + "\"";
                    RhinoApp.RunScript(command, false);

                    return true;
                }
            }

            return false;
        }
    }
}
