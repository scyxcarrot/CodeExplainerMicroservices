namespace IDS.Core.ImplantDirector
{
    public interface ICaseInfoProvider
    {
        int draft { get; set; }

        string caseId { get; set; }

        int version { get; set; }

        bool defectIsLeft { get; }
    }
}
