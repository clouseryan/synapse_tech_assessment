using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SynapseTechAssessment.Services.Utilities.Files;

public class FileReader(IOptions<FileReaderSettings> options, ILogger<FileReader> logger)
{
    public async Task<IEnumerable<string>> ReadFilesAsync(CancellationToken cancellationToken)
    {
        var fileContents = new List<string>();
        var files = Directory.GetFiles(options.Value.DirectoryPath);

        foreach (var file in files)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                fileContents.Add(content);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read file: {FilePath}", file);
            }
        }

        if (fileContents.Count == 0)
        {
            logger.LogError("All file reads failed. No files were successfully read from directory: {DirectoryPath}", options.Value.DirectoryPath);
            throw new InvalidOperationException($"All file reads failed in directory: {options.Value.DirectoryPath}");
        }

        return fileContents;
    }
}