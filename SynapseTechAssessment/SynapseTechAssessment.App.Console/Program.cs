using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SynapseTechAssessment.App.Console.HostedServices;
using SynapseTechAssessment.Services.LLMs.OpenAI;
using SynapseTechAssessment.Services.Utilities.Clients;
using SynapseTechAssessment.Services.Utilities.Files;
using SynapseTechAssessment.Services.Utilities.PhysicianNotes;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

var services = builder.Services;
var configuration = builder.Configuration;

services.Configure<OpenAiSettings>(configuration.GetSection("OpenAiSettings"));
services.Configure<HttpClientSettings>("OrderClientSettings",configuration.GetSection("OrderClientSettings"));
services.Configure<FileReaderSettings>(configuration.GetSection("FileReaderSettings"));
services.AddScoped<IPhysicianNoteExtractor, PhysicianNoteExtractor>();
services.AddHttpClient<OrderClient>();
services.AddScoped<IPhysicianNotesProcessor, PhysicianNotesProcessor>();

services.AddHostedService<PhysicianNotesFileWorker>();

var host = builder.Build();

await host.RunAsync();