using IDS.CMF.V2.ScrewQc;

namespace IDS.CMF.ScrewQc
{
    public class SkipOstDistAndIntersectResult: GenericScrewQcResult<SkipOstDistAndIntersectContent>
    {
        public SkipOstDistAndIntersectResult(string screwQcCheckName, SkipOstDistAndIntersectContent content) : base(screwQcCheckName, content)
        {
        }

        public override string GetQcBubbleMessage()
        {
            return string.Empty;
        }

        public override string GetQcDocTableCellMessage()
        {
            return string.Empty;
        }

        public override object GetSerializableScrewQcResult()
        {
            return new SkipOstDistAndIntersectContent(content);
        }

        public string SkipOstDistAndIntersectCheckMessage()
        {
            return content.SkipOstDistAndIntersectCheck? "No ost dist & int": string.Empty;
        }
    }
}
