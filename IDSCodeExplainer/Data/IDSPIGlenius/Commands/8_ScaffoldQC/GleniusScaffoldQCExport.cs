using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Glenius.CommandHelpers;
using IDS.Glenius.Enumerators;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("d94a9c0d-37f6-465e-9c40-cbd55c46ea3e")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.ScaffoldQC)]
    public class GleniusScaffoldQCExport : CommandBase<GleniusImplantDirector>
    {
        static GleniusScaffoldQCExport _instance;
        public GleniusScaffoldQCExport()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GleniusScaffoldQCExport command.</summary>
        public static GleniusScaffoldQCExport Instance => _instance;

        public override string EnglishName => "GleniusScaffoldQCExport";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var helper = new QCExportCommandHelper();

            return helper.DoQCExport(director, DocumentType.ScaffoldQC, false) ? Result.Success : Result.Failure;
        }
    }
}
