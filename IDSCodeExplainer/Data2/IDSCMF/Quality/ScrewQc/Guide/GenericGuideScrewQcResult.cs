using IDS.CMF.V2.ScrewQc;

namespace IDS.CMF.ScrewQc
{
    public abstract class GenericGuideScrewQcResult<TContent> : GenericScrewQcResult<TContent>, ISharedScrewQcResult
    {
        protected GenericGuideScrewQcResult(string screwQcCheckName, TContent content) : 
            base(screwQcCheckName, content)
        {
        }
        
        public abstract ISharedScrewQcResult CloneSharedScrewRelatedResult();
    }
}
