using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Operations;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.Importer;
using IDS.Core.PluginHelper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("2038ED6A-4C9C-4485-8426-D690AD236396")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide)]
    public class CMFImportGuideSupport : CmfCommandBase
    {
        public CMFImportGuideSupport()
        {
            TheCommand = this;
            VisualizationComponent = new CMFImportGuideSupportVisualizationComponent();
        }
        
        public static CMFImportGuideSupport TheCommand { get; private set; }
        
        public override string EnglishName => "CMFImportGuideSupport";
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            AllGuideFixationScrewGaugesProxy.Instance.IsEnabled = false;

            var mesh = StlImporter.ImportSingleStl();
            if (mesh == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "No guide support imported.");
                return Result.Failure;
            }

            doc.UndoRecordingEnabled = false;

            var guideSupportReplacement = new GuideSupportReplacement(director);
            var replaced = guideSupportReplacement.ReplaceGuideSupport(mesh, true);

            if (guideSupportReplacement.WarningReporting.Any())
            {
                foreach (var keyValuePair in guideSupportReplacement.WarningReporting)
                {
                    var warningString = string.Empty;
                    keyValuePair.Value.ForEach(x => { warningString += $"\n{x}"; });
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"{keyValuePair.Key}: {warningString}");
                }
            }

            if (guideSupportReplacement.ErrorReporting.Any())
            {
                var errorString = new StringBuilder("Import Guide Support failed to be imported due to the reasoning listed below.\n");

                var n = 1;
                foreach (var keyValuePair in guideSupportReplacement.ErrorReporting)
                {
                    errorString.Append($"\n{n}. {keyValuePair.Key}:");
                    keyValuePair.Value.ForEach(x => { errorString.Append($"\n{x}"); });
                    n++;
                    errorString.Append("\n");
                }

                errorString.Append("\n\nPlease fix your support mesh and make sure that at least:\n");
                errorString.Append("    i.  Intersecting triangles is 0.\n");
                errorString.Append("    ii. There are no holes in your support mesh.\n");
                errorString.Append("    iii.The support mesh created covered the area of where:\n");
                errorString.Append("         i.  Guide surface were drawn.\n");
                errorString.Append("         ii. Guide fixation screw, Guide Flange and Guide Bridge were positioned.\n");

                MessageBox.Show(errorString.ToString(), "Import Guide Support Aborted",MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            doc.UndoRecordingEnabled = true;

            if (replaced)
            {
                doc.ClearUndoRecords(true);
                doc.ClearRedoRecords();
            }

            return replaced ? Result.Success : Result.Failure;
        }
    }
}