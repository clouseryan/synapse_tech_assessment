using SynapseTechAssessment.Data.Models;

namespace SynapseTechAssessment.Services.LLMs.OpenAI;

public interface IPhysicianNoteExtractor
{
    Task<Order> ExtractOrderAsync(string note, CancellationToken cancellationToken);
}