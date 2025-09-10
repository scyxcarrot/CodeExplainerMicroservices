using IDS.CMF.V2.Logics;
using IDS.Core.Utilities;
using IDS.Interface.Logic;
using IDS.PICMF.Forms;

namespace IDS.PICMF.Helper
{
    public class ProPlanCheckHelper : IProPlanCheckHelper
    {
        public LogicStatus PrepareLogicParameters(out ProPlanCheckParameters parameters)
        {
            parameters = new ProPlanCheckParameters();

            if (!GetProPlanFilePath(out var filePath))
            {
                return LogicStatus.Cancel;
            }

            parameters.ProPlanFilePath = filePath;

            return LogicStatus.Success;
        }

        public LogicStatus ProcessLogicResult(ProPlanCheckResult result)
        {
            if (result.Parts == null)
            {
                return LogicStatus.Failure;
            }

            var ppc = new ProPlanCheck
            {
                Topmost = true
            };

            ppc.SetPartList(result.Parts);
            ppc.Show();

            return LogicStatus.Success;
        }

        private bool GetProPlanFilePath(out string filePath)
        {
            filePath = FileUtilities.GetFileDir("Please select an SPPC file", "SPPC files (*.sppc)|*.sppc||", string.Empty);

            return filePath != string.Empty;
        }
    }
}
