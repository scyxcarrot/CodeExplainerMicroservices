using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Relations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("C2093F02-40E6-4FBC-8ECD-8037CC9E8E95")]
    [IDSGleniusCommandAttribute(~DesignPhase.Draft, IBB.Scapula)]
    public class GleniusStartInitialization : CommandBase<GleniusImplantDirector>
    {
        public GleniusStartInitialization()
        {
            Instance = this;
            VisualizationComponent = new StartInitializationVisualization();
        }
        
        public static GleniusStartInitialization Instance { get; private set; }

        public override string EnglishName => "GleniusStartInitialization";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            const DesignPhase targetPhase = DesignPhase.Initialization;
            var success = PhaseChanger.ChangePhase(director, targetPhase, true);
            return !success ? Result.Failure : Result.Success;
        }
    }
}
