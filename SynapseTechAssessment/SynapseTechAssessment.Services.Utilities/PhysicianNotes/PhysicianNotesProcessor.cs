using Microsoft.Extensions.Logging;
using SynapseTechAssessment.Services.LLMs.OpenAI;
using SynapseTechAssessment.Services.Utilities.Clients;

namespace SynapseTechAssessment.Services.Utilities.PhysicianNotes;

public class PhysicianNotesProcessor(
    IPhysicianNoteExtractor physicianNoteExtractor,
    OrderClient orderClient,
    ILogger<PhysicianNotesProcessor> logger) : IPhysicianNotesProcessor
{

    public async Task ProcessPhysiciansNoteAsync(string note, CancellationToken cancellationToken)
    {
        try
        {
            var order = await physicianNoteExtractor.ExtractOrderAsync(note, cancellationToken);
            await orderClient.PostOrderAsync(order, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError("Error occured during processing of physician notes: {Error}.", e.Message);
            // todo - send to error queue
            throw;
        }
    }
}