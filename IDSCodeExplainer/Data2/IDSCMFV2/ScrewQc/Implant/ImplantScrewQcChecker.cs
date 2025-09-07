using IDS.Interface.Tools;

namespace IDS.CMF.V2.ScrewQc
{
    public abstract class ImplantScrewQcChecker : ScrewQcCheckerV2
    {
        protected ImplantScrewQcChecker(IConsole console, ImplantScrewQcCheck implantScrewQcCheckName) :
            base(console, implantScrewQcCheckName.ToString())
        {
        }
    }
}