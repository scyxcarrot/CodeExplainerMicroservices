using Microsoft.Extensions.VectorData;

namespace IDSCodeExplainer.Services.Ingestion;

public class SemanticSearch(
    VectorStoreCollection<Guid, CodeDocument> documentCollection,
    VectorStoreCollection<Guid, CodeChunk> chunkCollection)
{
    /// <summary>
    /// For an agentic LLM to search the RAG using Model Context Protocol
    /// </summary>
    /// <param name="searchText">search text</param>
    /// <param name="documentNameFilter">document name to filter</param>
    /// <param name="maxResults"></param>
    /// <returns>strings of the relevant search text</returns>
    public async Task<IEnumerable<VectorSearchResult<CodeChunk>>> SearchAsync(string searchText, string? documentNameFilter, int maxResults)
    {
        VectorSearchOptions<CodeChunk> searchOptions = new VectorSearchOptions<CodeChunk>();
        if (documentNameFilter != null && documentNameFilter.Length > 0)
        {
            List<CodeDocument> relevantDocuments = await documentCollection
                .GetAsync(document => document.RelativePath.Contains(documentNameFilter), top: int.MaxValue)
                .ToListAsync();

            List<string> relevantDocumentIds = relevantDocuments
                .Select(codeDocument => codeDocument.Id.ToString())
                .ToList();
            searchOptions.Filter = record => relevantDocumentIds.Contains(record.CodeDocumentId);
        }

        IAsyncEnumerable<VectorSearchResult<CodeChunk>> searchResults =
            chunkCollection.SearchAsync(searchText, maxResults, searchOptions);
        return await searchResults.ToListAsync();
    }

    public async Task<CodeDocument> GetDocument(string codeDocumentId)
    {
        CodeDocument codeDocument = await documentCollection
            .GetAsync(document => document.Id.ToString() == codeDocumentId, 1)
            .FirstAsync();

        return codeDocument;
    }
}
