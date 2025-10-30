using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SynapseTechAssessment.Data.Models;
using SynapseTechAssessment.Services.Utilities.PhysicianNotes;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace SynapseTechAssessment.App.Console.Tests;

[TestFixture]
public class PhysicianNotesProcessorIntegrationTests
{
    private CustomWebApplicationFactory _factory = null!;
    private IPhysicianNotesProcessor _processor = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new CustomWebApplicationFactory();
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task ProcessPhysiciansNoteAsync_HappyPath_SuccessfullyProcessesAndPostsOrder()
    {
        // Arrange
        var host = _factory.BuildHost();
        _processor = host.Services.GetRequiredService<IPhysicianNotesProcessor>();

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

        var testNote = "Patient John Doe needs a CPAP Machine for sleep apnea.";

        // Act
        await _processor.ProcessPhysiciansNoteAsync(testNote, CancellationToken.None);

        // Assert
        _factory.MockPhysicianNoteExtractor.Verify(
            x => x.ExtractOrderAsync(testNote, It.IsAny<CancellationToken>()),
            Times.Once);

        var requests = _factory.WireMockServer.LogEntries;
        Assert.That(requests.Count(), Is.EqualTo(1));
        Assert.That(requests.First().RequestMessage.Path, Is.EqualTo("/orders"));
        Assert.That(requests.First().RequestMessage.Method, Is.EqualTo("POST"));
    }

    [Test]
    public void ProcessPhysiciansNoteAsync_OrderClientReturns500_ThrowsException()
    {
        // Arrange
        var host = _factory.BuildHost();
        _processor = host.Services.GetRequiredService<IPhysicianNotesProcessor>();

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

        var testNote = "Patient needs wheelchair.";

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _processor.ProcessPhysiciansNoteAsync(testNote, CancellationToken.None));
    }

    [Test]
    public void ProcessPhysiciansNoteAsync_OrderClientReturns404_ThrowsException()
    {
        // Arrange
        var host = _factory.BuildHost();
        _processor = host.Services.GetRequiredService<IPhysicianNotesProcessor>();

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

        var testNote = "Patient needs oxygen tank.";

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _processor.ProcessPhysiciansNoteAsync(testNote, CancellationToken.None));
    }

    [Test]
    public void ProcessPhysiciansNoteAsync_ExtractorThrowsException_PropagatesException()
    {
        // Arrange
        var host = _factory.BuildHost();
        _processor = host.Services.GetRequiredService<IPhysicianNotesProcessor>();

        _factory.MockPhysicianNoteExtractor!
            .Setup(x => x.ExtractOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM service unavailable"));

        var testNote = "Some physician note.";

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _processor.ProcessPhysiciansNoteAsync(testNote, CancellationToken.None));
    }

    [Test]
    public void ProcessPhysiciansNoteAsync_RequestTimeout_ThrowsTaskCanceledException()
    {
        // Arrange
        var host = _factory.BuildHost();
        _processor = host.Services.GetRequiredService<IPhysicianNotesProcessor>();

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
                .WithDelay(TimeSpan.FromSeconds(60))
                .WithStatusCode(HttpStatusCode.OK));

        var testNote = "Patient needs walker.";

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _processor.ProcessPhysiciansNoteAsync(testNote, CancellationToken.None));
    }
}
