using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SynapseTechAssessment.App.Console.HostedServices;
using SynapseTechAssessment.Services.LLMs.OpenAI;
using SynapseTechAssessment.Services.Utilities.Clients;
using SynapseTechAssessment.Services.Utilities.Files;
using SynapseTechAssessment.Services.Utilities.PhysicianNotes;
using WireMock.Server;

namespace SynapseTechAssessment.App.Console.Tests;

public class CustomWebApplicationFactory : IDisposable
{
    private IHost? _host;
    private WireMockServer? _wireMockServer;

    public Mock<IPhysicianNoteExtractor>? MockPhysicianNoteExtractor { get; private set; }
    public WireMockServer? WireMockServer => _wireMockServer;

    public void BuildHost(string testDataDirectory, Action<IServiceCollection>? configureServices = null)
    {
        _wireMockServer = WireMockServer.Start();

        var builder = Host.CreateApplicationBuilder();

        // Configure logging
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        // Copy settings from Program.cs but override for testing
        builder.Services.Configure<OpenAiSettings>(options =>
        {
            options.ModelName = "test-model";
            options.ApiKey = "test-key";
            options.Host = "http://localhost:1234";
        });

        builder.Services.Configure<HttpClientSettings>("OrderClientSettings", options =>
        {
            options.Host = _wireMockServer.Url!;
            options.ClientTimeout = 3;
        });

        builder.Services.Configure<FileReaderSettings>(options =>
        {
            options.DirectoryPath = testDataDirectory;
        });

        // Mock IPhysicianNoteExtractor
        MockPhysicianNoteExtractor = new Mock<IPhysicianNoteExtractor>();
        builder.Services.AddScoped<IPhysicianNoteExtractor>(_ => MockPhysicianNoteExtractor.Object);

        // Real services
        builder.Services.AddHttpClient<OrderClient>();
        builder.Services.AddScoped<IPhysicianNotesProcessor, PhysicianNotesProcessor>();
        builder.Services.AddScoped<FileReader>();

        // Register the hosted service
        builder.Services.AddScoped<PhysicianNotesFileWorker>();

        // Allow custom service configuration
        configureServices?.Invoke(builder.Services);

        _host = builder.Build();
    }

    public IServiceScope GetScope()
    {
        if (_host == null)
            throw new InvalidOperationException("Host not built.");

        return _host.Services.CreateScope();
    }

    public void Dispose()
    {
        _host?.Dispose();
        _wireMockServer?.Stop();
        _wireMockServer?.Dispose();
    }
}
