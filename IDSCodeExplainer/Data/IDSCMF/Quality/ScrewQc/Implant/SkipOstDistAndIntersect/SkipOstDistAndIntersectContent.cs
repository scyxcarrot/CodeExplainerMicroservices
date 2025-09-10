using Newtonsoft.Json;

namespace IDS.CMF.ScrewQc
{
    public class SkipOstDistAndIntersectContent
    {
        [JsonProperty("sc")]
        public bool SkipOstDistAndIntersectCheck { get; }

        public SkipOstDistAndIntersectContent()
        {
            SkipOstDistAndIntersectCheck = false;
        }

        public SkipOstDistAndIntersectContent(bool skipOstDistAndIntersectCheck)
        {
            SkipOstDistAndIntersectCheck = skipOstDistAndIntersectCheck;
        }

        public SkipOstDistAndIntersectContent(SkipOstDistAndIntersectContent sourceContent)
        {
            SkipOstDistAndIntersectCheck = sourceContent.SkipOstDistAndIntersectCheck;
        }
    }
}
