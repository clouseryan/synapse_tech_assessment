using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SynapseTechAssessment.Services.Utilities.PhysicianNotes;

namespace SynapseTechAssessment.App.Console.HostedServices;

public class PhysicianNotesFileWorker(IPhysicianNotesProcessor physicianNotesProcessor, ILogger<PhysicianNotesFileWorker> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}