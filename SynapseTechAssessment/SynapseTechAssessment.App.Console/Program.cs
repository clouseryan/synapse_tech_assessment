using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SynapseTechAssessment.Services.LLMs.OpenAI;
using SynapseTechAssessment.Services.Utilities.Clients;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

var services = builder.Services;
var configuration = builder.Configuration;

services.Configure<OpenAiSettings>(configuration.GetSection("OpenAiSettings"));
services.Configure<HttpClientSettings>("OrderClientSettings",configuration.GetSection("OrderClientSettings"));
services.AddSingleton<IPhysicianNoteExtractor, PhysicianNoteExtractor>();

var host = builder.Build();

await host.RunAsync();