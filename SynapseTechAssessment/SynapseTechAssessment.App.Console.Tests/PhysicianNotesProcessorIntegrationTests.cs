using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SynapseTechAssessment.App.Console.HostedServices;
using SynapseTechAssessment.Data.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace SynapseTechAssessment.App.Console.Tests;

[TestFixture]
public class PhysicianNotesProcessorIntegrationTests
{
    private CustomWebApplicationFactory _factory = null!;
    private PhysicianNotesFileWorker _worker = null!;
    private string _testDataDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new CustomWebApplicationFactory();
        _testDataDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDirectory);
        _factory.BuildHost(_testDataDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
        if (Directory.Exists(_testDataDirectory))
        {
            Directory.Delete(_testDataDirectory, true);
        }
    }

    private void CreateTestFile(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(_testDataDirectory, fileName), content);
    }

    [Test]
    public async Task ProcessPhysiciansNoteAsync_HappyPath_SuccessfullyProcessesAndPostsOrder()
    {
        // Arrange
        CreateTestFile("physician_note1.txt", "Patient John Doe needs a CPAP Machine for sleep apnea.");

        _worker = _factory.GetScope().ServiceProvider.GetRequiredService<PhysicianNotesFileWorker>();

        var expectedOrder = new Order
        {
            Device = "CPAP Machine",
            Liters = "",
            Usage = "Nightly during sleep",
            Diagnosis = "Obstructive Sleep Apnea",
            OrderingProvider = "Dr. Smith",
            PatientName = "John Doe",
            Dob = "1980-01-15"
        };

        _factory.MockPhysicianNoteExtractor!
            .Setup(x => x.ExtractOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        _factory.WireMockServer!
            .Given(Request.Create()
                .WithPath("/orders")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK));

        // Act
        await _worker.StartAsync(CancellationToken.None);

        // Assert
        _factory.MockPhysicianNoteExtractor.Verify(
            x => x.ExtractOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        var requests = _factory.WireMockServer.LogEntries;
        Assert.That(requests.Count(), Is.EqualTo(1));
        Assert.That(requests.First().RequestMessage.Path, Is.EqualTo("/orders"));
        Assert.That(requests.First().RequestMessage.Method, Is.EqualTo("POST"));
    }

    [Test]
    public async Task ProcessPhysiciansNoteAsync_OrderClientReturns500_LogsError()
    {
        // Arrange
        CreateTestFile("physician_note2.txt", "Patient needs wheelchair.");

        var mockLogger = new Mock<ILogger<PhysicianNotesFileWorker>>();

        _factory.BuildHost(_testDataDirectory, services =>
        {
            services.AddScoped<ILogger<PhysicianNotesFileWorker>>(_ => mockLogger.Object);
        });

        _worker = _factory.GetScope().ServiceProvider.GetRequiredService<PhysicianNotesFileWorker>();

        var expectedOrder = new Order
        {
            Device = "Wheelchair",
            OrderingProvider = "Dr. Jones",
            PatientName = "Jane Smith",
            Dob = "1975-05-20"
        };

        _factory.MockPhysicianNoteExtractor!
            .Setup(x => x.ExtractOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        _factory.WireMockServer!
            .Given(Request.Create()
                .WithPath("/orders")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.InternalServerError)
                .WithBody("Internal Server Error"));

        // Act
        await _worker.StartAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to process file")),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessPhysiciansNoteAsync_OrderClientReturns404_LogsError()
    {
        // Arrange
        CreateTestFile("physician_note3.txt", "Patient needs oxygen tank.");

        var mockLogger = new Mock<ILogger<PhysicianNotesFileWorker>>();

        _factory.BuildHost(_testDataDirectory, services =>
        {
            services.AddScoped<ILogger<PhysicianNotesFileWorker>>(_ => mockLogger.Object);
        });

        _worker = _factory.GetScope().ServiceProvider.GetRequiredService<PhysicianNotesFileWorker>();

        var expectedOrder = new Order
        {
            Device = "Oxygen Tank",
            Liters = "10",
            OrderingProvider = "Dr. Brown",
            PatientName = "Bob Johnson",
            Dob = "1960-08-10"
        };

        _factory.MockPhysicianNoteExtractor!
            .Setup(x => x.ExtractOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        _factory.WireMockServer!
            .Given(Request.Create()
                .WithPath("/orders")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NotFound)
                .WithBody("Not Found"));

        // Act
        await _worker.StartAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to process file")),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessPhysiciansNoteAsync_ExtractorThrowsException_LogsError()
    {
        // Arrange
        CreateTestFile("physician_note4.txt", "Some physician note.");

        var mockLogger = new Mock<ILogger<PhysicianNotesFileWorker>>();

        _factory.BuildHost(_testDataDirectory, services =>
        {
            services.AddScoped<ILogger<PhysicianNotesFileWorker>>(_ => mockLogger.Object);
        });

        _worker = _factory.GetScope().ServiceProvider.GetRequiredService<PhysicianNotesFileWorker>();

        _factory.MockPhysicianNoteExtractor!
            .Setup(x => x.ExtractOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM service unavailable"));

        // Act
        await _worker.StartAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to process file")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessPhysiciansNoteAsync_RequestTimeout_LogsError()
    {
        // Arrange
        CreateTestFile("physician_note5.txt", "Patient needs walker.");

        var mockLogger = new Mock<ILogger<PhysicianNotesFileWorker>>();

        _factory.BuildHost(_testDataDirectory, services =>
        {
            services.AddScoped<ILogger<PhysicianNotesFileWorker>>(_ => mockLogger.Object);
        });

        _worker = _factory.GetScope().ServiceProvider.GetRequiredService<PhysicianNotesFileWorker>();

        var expectedOrder = new Order
        {
            Device = "Walker",
            OrderingProvider = "Dr. White",
            PatientName = "Alice Green",
            Dob = "1985-12-25"
        };

        _factory.MockPhysicianNoteExtractor!
            .Setup(x => x.ExtractOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        _factory.WireMockServer!
            .Given(Request.Create()
                .WithPath("/orders")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithDelay(TimeSpan.FromSeconds(5))
                .WithStatusCode(HttpStatusCode.OK));

        // Act
        await _worker.StartAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to process file")),
                It.IsAny<TaskCanceledException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
