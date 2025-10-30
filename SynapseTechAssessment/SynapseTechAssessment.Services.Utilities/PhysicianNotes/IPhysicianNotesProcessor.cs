namespace SynapseTechAssessment.Services.Utilities.PhysicianNotes;

public interface IPhysicianNotesProcessor
{
    Task ProcessPhysiciansNoteAsync(string note, CancellationToken cancellationToken);
}