using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Utilities;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("345FA8BA-99DC-4721-8A3E-C710ED5103AE")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant)]
    public class CMFCreateImplantSupportTransition : CmfCommandBase
    {
        public CMFCreateImplantSupportTransition()
        {
            TheCommand = this;
            VisualizationComponent = new CMFImplantSupportTransitionVisualization();
        }

        public static CMFCreateImplantSupportTransition TheCommand { get; private set; }

        public override string EnglishName => "CMFCreateImplantSupportTransition";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var getter = new ImplantTransitionInputGetter(director, VisualizationComponent as IImplantTransitionVisualization);
            var result = getter.GetInputs(out var implantTransitionDictionary);

            if (result != Result.Success)
            {
                return result;
            }

            var helper = new ImplantTransitionObjectHelper(director);

            foreach (var output in implantTransitionDictionary)
            {
                helper.AddNewTransition(output.Value, output.Key);
            }

            return Result.Success;
        }
    }
}
