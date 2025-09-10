namespace IDS.Core.Quality
{
    public interface IQCResources
    {
        string qcDocumentCssFile { get; }
        string qcDocumentCssTestVersionFile { get; }

        string qcDocumentHtmlFile { get; }

        string qcDocumentJavaScriptFile { get; }
    }
}