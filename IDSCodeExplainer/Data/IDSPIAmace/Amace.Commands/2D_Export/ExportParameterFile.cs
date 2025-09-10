using IDS.Amace.FileSystem;
using IDS.Core.CommandBase;
using IDS.Operations.Export;
using Rhino;
using Rhino.Commands;
using System.Windows.Forms;

namespace IDS.Amace.Commands
{
    [System.Runtime.InteropServices.Guid("09fbd2ad-ec7b-40b4-bd5b-ec4035a06fa2")]
    public class ExportParameterFile : CommandBase<ImplantDirector>
    {
        public ExportParameterFile()
        {
            Instance = this;
        }

        ///<summary>The only instance of the ExportParameterFile command.</summary>
        public static ExportParameterFile Instance { get; private set; }

        public override string EnglishName => "ExportParameterFile";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            string parameterFile;

            try
            {
                // Export the parameter file to the work directory
                parameterFile =
                    $"{DirectoryStructure.GetWorkingDir(director.Document)}\\{director.Inspector.CaseId}_Design_Parameters_v{director.version:D}_draft{director.draft:D}.txt";
            }
            catch
            {
                // If automatic file selection fails, ask user for the output location (will be
                // triggered if no director exists)
                var dlg = new OpenFileDialog
                {
                    Filter = "Design Parameter File (*.txt)|*.txt",
                    CheckFileExists = false
                };
                dlg.ShowDialog();
                if (dlg.FileName != string.Empty)
                {
                    parameterFile = dlg.FileName;
                }
                else
                {
                    return Result.Failure;
                }
            }

            // Export the file
            ParameterExporter.ExportParameterFile(director, parameterFile);

            return Result.Success;
        }
    }
}