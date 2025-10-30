using Microsoft.Extensions.Logging;
using SynapseTechAssessment.Services.LLMs.OpenAI;

namespace SynapseTechAssessment.Services.Utilities.PhysicianNotes;

public class PhysicianNotesProcessor(
    IPhysicianNoteExtractor physicianNoteExtractor,
    ILogger<PhysicianNotesProcessor> logger)
{

    public async Task ProcessPhysicianNotesAsync(string note, CancellationToken cancellationToken)
    {

    }
}