using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Relations;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("3dedab31-b5c6-4125-bd6f-ab9738e097d0")]
    [IDSGleniusCommandAttribute(~DesignPhase.Draft, IBB.Scapula)]
    public class GleniusStartReconstruction : CommandBase<GleniusImplantDirector>
    {
        public GleniusStartReconstruction()
        {
            Instance = this;
        }

        ///<summary>The only instance of the Reconstruction command.</summary>
        public static GleniusStartReconstruction Instance { get; private set; }

        public override string EnglishName => "GleniusReconstruction";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            const DesignPhase targetPhase = DesignPhase.Reconstruction;
            var success = PhaseChanger.ChangePhase(director, targetPhase, true);
            return !success ? Result.Failure : Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, GleniusImplantDirector director)
        {
            RhinoApp.WriteLine("Successfully perform Glenius reconstruction.");
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, GleniusImplantDirector director)
        {
            RhinoApp.WriteLine("Glenius Reconstruction failed.");
        }
    }
}
