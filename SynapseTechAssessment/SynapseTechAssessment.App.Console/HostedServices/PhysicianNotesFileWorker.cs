using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SynapseTechAssessment.Services.Utilities.Files;
using SynapseTechAssessment.Services.Utilities.PhysicianNotes;

namespace SynapseTechAssessment.App.Console.HostedServices;

public class PhysicianNotesFileWorker(
    IPhysicianNotesProcessor physicianNotesProcessor,
    ILogger<PhysicianNotesFileWorker> logger,
    FileReader fileReader) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting physician notes file worker.");

        try
        {
            logger.LogInformation("Reading physician notes files.");
            var files = await fileReader.ReadFilesAsync(cancellationToken);
            logger.LogInformation("Found {FileCount} physician notes files to process.", files.Count());

            var processedCount = 0;
            var failedCount = 0;

            foreach (var file in files)
            {
                try
                {
                    logger.LogInformation("Processing file: {File}", file);
                    await physicianNotesProcessor.ProcessPhysiciansNoteAsync(file, cancellationToken);
                    processedCount++;
                    logger.LogInformation("Successfully processed file: {File}", file);
                }
                catch (Exception fileException)
                {
                    failedCount++;
                    logger.LogError(fileException, "Failed to process file: {File}. Error: {Error}", file, fileException.Message);
                }
            }

            logger.LogInformation("Physician notes processing completed. Processed: {ProcessedCount}, Failed: {FailedCount}, Total: {TotalCount}",
                processedCount, failedCount, files.Count());
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred during processing of physician notes: {Error}", e.Message);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(cancellationToken.IsCancellationRequested
            ? "Cancellation requested. Stopping physician notes file worker."
            : "Stopping physician notes file worker.");
    }
}