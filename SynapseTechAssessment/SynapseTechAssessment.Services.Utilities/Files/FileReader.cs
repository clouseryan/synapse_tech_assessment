using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SynapseTechAssessment.Services.Utilities.Files;

public class FileReader(IOptions<FileReaderSettings> options, ILogger<FileReader> logger)
{
    public async Task<IEnumerable<string>> ReadFilesAsync(CancellationToken cancellationToken)
    {

    }
}