namespace IDS.CMF.V2.ScrewQc
{
    public interface IScrewQcResult
    {
        string GetScrewQcCheckName();

        // No message if pass
        string GetQcBubbleMessage();

        string GetQcDocTableCellMessage();

        object GetSerializableScrewQcResult();
    }
}
