using System.ClientModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using SynapseTechAssessment.Data.Models;

namespace SynapseTechAssessment.Services.LLMs.OpenAI;

public class PhysicianNoteExtractor : IPhysicianNoteExtractor
{
    private readonly OpenAiSettings _settings;
    private readonly OpenAIClient _client;
    private readonly ILogger<PhysicianNoteExtractor> _logger;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };


    public PhysicianNoteExtractor(IOptions<OpenAiSettings> settingOptions, ILogger<PhysicianNoteExtractor> logger)
    {
        _settings = settingOptions.Value;
        _logger = logger;

        _client = new OpenAIClient(new ApiKeyCredential(_settings.ApiKey), new OpenAIClientOptions()
        {
            Endpoint = new Uri(_settings.Host)
        });
    }

    public async Task<Order> ExtractOrderAsync(string note, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting order extraction from physician note. Note length: {NoteLength} characters", note?.Length ?? 0);

        try
        {
            var chatClient = _client.GetChatClient(_settings.ModelName);
            _logger.LogDebug("Created chat client for model: {ModelName}", _settings.ModelName);

            var systemPrompt = """
                               You are a medical data extraction assistant. Extract the following information from physician notes:
                               - Device: The medical device being ordered
                               - Liters: The size in liters (ONLY if the device is an oxygen tank, otherwise leave empty)
                               - Usage: How often or how the device should be used
                               - Diagnosis: The medical diagnosis or condition
                               - OrderingProvider: The name of the doctor ordering the device
                               - PatientName: The patient's full name
                               - Dob: The patient's date of birth

                               Return the data in JSON format matching this structure:
                               {
                                 "Device": "device name",
                                 "Liters": "liters or empty string",
                                 "Usage": "usage instructions",
                                 "Diagnosis": "diagnosis",
                                 "OrderingProvider": "provider name",
                                 "PatientName": "patient name",
                                 "Dob": "date of birth"
                               }

                               If any field is not found in the note, use an empty string.
                               """;

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(note)
            };

            var chatCompletionOptions = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            _logger.LogDebug("Sending chat completion request to OpenAI");
            var response = await chatClient.CompleteChatAsync(messages, chatCompletionOptions, cancellationToken);
            _logger.LogInformation("Successfully received response from OpenAI");

            var jsonResponse = response.Value.Content[0].Text;
            _logger.LogDebug("Received JSON response: {JsonResponse}", jsonResponse);

            var order = JsonSerializer.Deserialize<Order>(jsonResponse, _jsonSerializerOptions);

            if (order == null)
            {
                _logger.LogWarning("Deserialization resulted in null order, returning empty order");
                return new Order();
            }

            _logger.LogInformation("Successfully extracted order. Device: {Device}, OrderingProvider: {OrderingProvider}",
                order.Device, order.OrderingProvider);

            return order;
        }
        catch (ClientResultException ex)
        {
            _logger.LogError(ex, "OpenAI API error occurred during order extraction. Status: {Status}", ex.Status);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON response from OpenAI");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during order extraction");
            throw;
        }
    }
}