using System.Collections.Generic;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.PICMF
{
    public abstract class CMFExportSupportSourcesBase : CmfCommandBase
    {
        private const string Yes = "Yes";
        private const string No = "No";

        protected bool ExportMxp { get; set; } = false;

        protected bool CheckIfMxpShouldBeExported()
        {
            var go = new GetOption();
            go.SetCommandPrompt("Do you want to export structured MXP files?");
            go.AcceptNothing(true);

            var options = new List<string>() { Yes, No };
            go.AddOptionList("ExportMxp", options, 0);
            var exportMxp = Yes;

            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    return false;
                }

                if (res == GetResult.Option)
                {
                    exportMxp = options[go.Option().CurrentListOptionIndex];
                    break;
                }

                if (res == GetResult.Nothing)
                {
                    break;
                }
            }

            ExportMxp = exportMxp == Yes;
            return true;
        }
    }
}
