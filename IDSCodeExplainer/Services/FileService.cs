using System.Text.RegularExpressions;

namespace IDSCodeExplainer.Services
{
    public class FileService(
        IWebHostEnvironment webHostEnvironment, 
        ILogger<FileService> logger)
    {
        // Base path to restrict file access
        private readonly string _basePath = Path.Combine(webHostEnvironment.ContentRootPath, "Data");

        /// <summary>
        /// Reads the content of a specified file.
        /// </summary>
        /// <param name="filename">The name of the file to read (must be within the allowed base path).</param>
        /// <returns>The content of the file, or an error message if the file cannot be read.</returns>
        public async Task<string> ReadFileContentAsync(string filename)
        {
            try
            {
                // Normalize and validate the path to prevent directory traversal attacks
                string fullPath = Path.Combine(_basePath, filename);
                if (!Path.GetFullPath(fullPath).StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning($"Attempted to read file outside allowed path: {filename}");
                    return "Error: Access denied. File not found or outside permitted directory.";
                }
                if (!File.Exists(fullPath))
                {
                    logger.LogWarning($"File not found: {fullPath}");
                    return $"Error: File '{filename}' not found.";
                }

                logger.LogInformation($"Reading file: {fullPath}");
                return await File.ReadAllTextAsync(fullPath);
            }
            catch (IOException ex)
            {
                logger.LogError(ex, $"IO error reading file '{filename}'.");
                return $"Error reading file '{filename}': {ex.Message}";
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogError(ex, $"Unauthorized access to file '{filename}'.");
                return $"Error: Unauthorized access to file '{filename}'.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An unexpected error occurred while reading file '{filename}'.");
                return $"An unexpected error occurred while reading file '{filename}'.";
            }
        }

        public List<string> FindFile(string searchPattern)
        {
            var matchingFiles = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(searchPattern))
                {
                    logger.LogWarning("FindFile called with empty search term.");
                }

                logger.LogInformation($"Searching for files matching '{searchPattern}' in '{_basePath}'");

                // replace space with *
                var regexPattern = searchPattern.Replace(" ", "*");

                matchingFiles = Directory.EnumerateFiles(
                        _basePath, "*", SearchOption.AllDirectories) 
                    .Where(file => Regex.IsMatch(Path.GetFileName(file), regexPattern, RegexOptions.IgnoreCase))
                    .ToList();
            }
            catch (DirectoryNotFoundException ex)
            {
                logger.LogError(ex, $"Base directory not found: {_basePath}");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogError(ex, $"Unauthorized access to directory: {_basePath}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An unexpected error occurred during file search in '{_basePath}'.");
            }

            return matchingFiles;
        }
    }
}
