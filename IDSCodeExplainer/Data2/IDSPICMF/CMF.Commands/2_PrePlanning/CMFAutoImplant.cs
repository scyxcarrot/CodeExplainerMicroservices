using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantProposal;
using IDS.Core.Enumerators;
using IDS.PICMF.Helper;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("38b2a7bd-4144-4a5f-b3cc-cab3944f06ad")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Planning)]
    public class CMFAutoImplant: CmfCommandBase
    {
        public CMFAutoImplant()
        {
            TheCommand = this;
            SubscribedLoadEvent = false;
        }

        public static CMFAutoImplant TheCommand { get; private set; }

        public override string EnglishName => "CMFAutoImplant";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var implantProposalInput = new ImplantProposalInput(director);
            var success = implantProposalInput
                .GetImplantPreferenceModel(out var implantPreferenceModel);
            if (!success)
            {
                return Result.Failure;
            }
            // add else if at the bottom for other implant proposal
            if (implantPreferenceModel.CasePrefData.ImplantTypeValue == ImplantProposalOperations.Genio)
            {
                var implantProposalGenioModel = ImplantProposalGenioModel.Default();

                success = implantProposalInput.GetUserInputs(ref implantProposalGenioModel);
                if (!success)
                {
                    return Result.Failure;
                }

                ShowLoadIndicator(true);
                var implantProposalGenio = new ImplantProposalGenio(director);
                success = implantProposalGenio.PerformGenioImplantProposal(
                    implantProposalGenioModel,
                    ref implantPreferenceModel,
                    out var autoImplantProposalResult);

                if (!success)
                {
                    ShowLoadIndicator(false);
                    return Result.Failure;
                }

                var implantProposalOutput = new ImplantProposalOutput(director);
                implantProposalOutput.CreateScrewsAndDotPastilles(
                    autoImplantProposalResult, ref implantPreferenceModel);
                ShowLoadIndicator(false);
            }
            else
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error,
                    $"Implant Proposal for " +
                    $"{implantPreferenceModel.CasePrefData.ImplantTypeValue} " +
                    $"is not supported");
                return Result.Failure;
            }

            return Result.Success;
        }
    }
}
