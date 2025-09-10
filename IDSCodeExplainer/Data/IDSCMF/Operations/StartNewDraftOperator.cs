using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.Relations;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using System.Collections.Generic;
using System.IO;

namespace IDS.CMF.Operations
{
    public class StartNewDraftOperator
    {
        private DocumentType _tmpDocumentType;
        private List<string> _tempInputFiles;

        public bool Execute(RhinoDoc doc, CMFImplantDirector director)
        {
            string workingDirPath;
            if (DirectoryStructure.CreateWorkingDir(doc, new List<string>() { }, new List<string>() { Path.GetFileName(doc.Path) }, new List<string>() { "3dm" }, out workingDirPath))
            {
                SaveDirectorState(director);

                // Set appropriate phase
                if (director.documentType == DocumentType.PlanningQC)
                {
                    director.draft++;
                    PhaseChanger.ChangePhase(director, DesignPhase.PlanningQC, false);
                }
                else if (director.documentType == DocumentType.MetalQC)
                {
                    director.draft++;
                    PhaseChanger.ChangePhase(director, DesignPhase.MetalQC, false);
                }
                else if (director.documentType == DocumentType.ApprovedQC)
                {
                    director.version++;
                    director.draft = 1;
                    PhaseChanger.ChangePhase(director, DesignPhase.MetalQC, false);
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "This case document type is not valid! Please ensure you obtained this case from a valid source!");
                    RestoreDirectorState(director);
                    return false;
                }

                director.documentType = DocumentType.Work;
                director.InputFiles = new List<string> { director.Document.Path };

                var options = new Rhino.FileIO.FileWriteOptions();
                var workFileName = Path.Combine(workingDirPath, $"{director.caseId}_work_v{director.version:D}_draft{director.draft:D}.3dm");
                if (director.Document.WriteFile(workFileName, options))
                {
                    File.SetAttributes(workFileName, FileAttributes.Normal);
                    // Discard changes and open the work file ('No' to suppress 'Save Changes' - only when document was modified)
                    var command = $"-_Open {(director.Document.Modified ? "No" : string.Empty)} \"{workFileName}\"";
                    RhinoApp.RunScript(command, false);

                    return true;
                }
                else
                {
                    RestoreDirectorState(director);
                }
            }

            return false;
        }

        private void SaveDirectorState(CMFImplantDirector director)
        {
            _tmpDocumentType = director.documentType;
            _tempInputFiles = director.InputFiles;
        }

        private void RestoreDirectorState(CMFImplantDirector director)
        {
            director.documentType = _tmpDocumentType;
            director.InputFiles = _tempInputFiles;
            director.OnInitialView(director.Document);
        }
    }
}
