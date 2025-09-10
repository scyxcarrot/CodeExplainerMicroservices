namespace IDS.CMF.DataModel
{
    internal struct DotSearchResult
    {
        public int ScrewIndex;
        public bool FoundLocation;
        public bool IsPastille;
        public bool HasScrewInfo;

        public static DotSearchResult CreateDefault()
        {
            return new DotSearchResult
            {
                ScrewIndex = -1,
                FoundLocation = false,
                IsPastille = false,
                HasScrewInfo = false
            };
        }
    }
}
