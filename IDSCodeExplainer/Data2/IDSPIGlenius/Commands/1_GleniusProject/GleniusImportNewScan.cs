using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("17EA97B8-9C05-4891-8A0A-E974F1A3C47F")]
    [IDSGleniusCommand(DesignPhase.Draft)]
    [CommandStyle(Style.ScriptRunner)]
    public class GleniusImportNewScan : CommandBase<GleniusImplantDirector>
    {
        public GleniusImportNewScan()
        {
            TheCommand = this;
        }

        public static GleniusImportNewScan TheCommand { get; private set; }

        public override string EnglishName => "GleniusImportNewScan";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var importNewScanOperator = new ImportNewScanOperator();
            var success = importNewScanOperator.Execute(doc, director);

            var activeDoc = RhinoDoc.ActiveDoc;
            activeDoc.ClearUndoRecords(true);
            activeDoc.ClearRedoRecords();

            return success ? Result.Success : Result.Failure;
        }

    }
}