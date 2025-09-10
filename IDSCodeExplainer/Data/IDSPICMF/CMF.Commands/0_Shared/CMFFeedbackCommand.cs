using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.PICMF.Forms;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("F51B7805-6FD8-471C-9678-498B17B189D6")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMFFeedbackCommand : CmfCommandBase
    {
        public CMFFeedbackCommand()
        {
            Instance = this;
        }

        public static CMFFeedbackCommand Instance { get; private set; }

        public override string EnglishName => "CMFFeedbackCommand";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var formId = "1FAIpQLSdwIRxrVBYJMWKOJWJbHrfV-D1P21vrC7pB1K-Mo9nUDREySA";
            var feedbackUrl = $"https://docs.google.com/forms/d/e/{formId}/viewform";

            var feedbackForm = new FeedbackForm
            {
                InitialUrl = feedbackUrl
            };
            feedbackForm.ShowDialog();

            return Result.Success;
        }
    }
}
