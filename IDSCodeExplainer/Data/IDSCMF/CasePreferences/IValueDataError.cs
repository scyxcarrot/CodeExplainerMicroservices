namespace IDS.CMF.CasePreferences
{
    public interface IValueDataError
    {
        bool HasPlateThicknessError { get; set; }
        bool HasPlateWidthError { get; set; }
        bool HasLinkWidthError { get; set; }
    }
}