using System.Security.Cryptography;
using System.Text;

namespace IDSCodeExplainer.Services.Ingestion;

public class CodeFileDirectorySource(string sourceDirectory) : IIngestionSource
{
    public string SourceFileId(string path) => Path.GetRelativePath(sourceDirectory, path);

    public string SourceId => $"{nameof(CodeFileDirectorySource)}:{sourceDirectory}";

    private static string SourceFileHashsum(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        return Encoding.Default.GetString(md5.ComputeHash(stream));
    }

    public Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var results = new List<IngestedDocument>();
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);
        var existingDocumentsById = existingDocuments.ToDictionary(d => d.DocumentId);

        foreach (var sourceFile in sourceFiles)
        {
            var sourceFileId = SourceFileId(sourceFile);
            var sourceFileHashsum = SourceFileHashsum(sourceFile);
            var existingDocumentHashsum = existingDocumentsById.TryGetValue(sourceFileId, out var existingDocument) ? existingDocument.DocumentVersion : null;
            if (existingDocumentHashsum != sourceFileHashsum)
            {
                results.Add(new() { Key = Guid.CreateVersion7().ToString(), SourceId = SourceId, DocumentId = sourceFileId, DocumentVersion = sourceFileHashsum });
            }
        }

        return Task.FromResult((IEnumerable<IngestedDocument>)results);
    }

    public Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var currentFiles = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);
        var currentFileIds = currentFiles.ToLookup(SourceFileId);
        var deletedDocuments = existingDocuments.Where(d => !currentFileIds.Contains(d.DocumentId));
        return Task.FromResult(deletedDocuments);
    }

    public Task<IEnumerable<IngestedChunk>> CreateChunksForDocumentAsync(IngestedDocument document)
    {
        var filePath = Path.Combine(sourceDirectory, document.DocumentId);
        var codeString = File.ReadAllText(filePath);
        var recursiveCodeSplitter = new RecursiveCodeSplitter(200, 20);
        var splitCodeStrings = recursiveCodeSplitter.SplitText(codeString);
        var chunks = splitCodeStrings.Select(splitCodeString => new IngestedChunk()
        {
            Key = Guid.CreateVersion7().ToString(),
            DocumentId = document.DocumentId,
            Text = splitCodeString
        });

        return Task.FromResult(chunks);
    }
}
