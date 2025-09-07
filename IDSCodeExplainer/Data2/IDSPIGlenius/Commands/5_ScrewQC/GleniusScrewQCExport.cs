using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Glenius.CommandHelpers;
using IDS.Glenius.Enumerators;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("1632268e-a8a9-4a74-b351-566a05c9d182")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.ScrewQC)]
    public class GleniusScrewQCExport : CommandBase<GleniusImplantDirector>
    {
        static GleniusScrewQCExport _instance;
        public GleniusScrewQCExport()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GleniusScrewQCExport command.</summary>
        public static GleniusScrewQCExport Instance => _instance;

        public override string EnglishName => "GleniusScrewQCExport";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var helper = new QCExportCommandHelper();

            return helper.DoQCExport(director, DocumentType.ScrewQC, false) ? Result.Success : Result.Failure;
        }
    }
}
