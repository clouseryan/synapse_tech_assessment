using System.ClientModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using SynapseTechAssessment.Data.Models;

namespace SynapseTechAssessment.Services.LLMs.OpenAI;

public class PhysicianNoteExtractor(
    OpenAIClient client,
    ILogger<PhysicianNoteExtractor> logger,
    IOptions<OpenAiSettings> settings) : IPhysicianNoteExtractor
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Order> ExtractOrderAsync(string note, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting order extraction from physician note. Note length: {NoteLength} characters",
            note?.Length ?? 0);

        try
        {
            var chatClient = client.GetChatClient(settings.Value.ModelName);
            logger.LogDebug("Created chat client for model: {ModelName}", settings.Value.ModelName);

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
                new UserChatMessage($"physicians note: {note}")
            };

            var chatCompletionOptions = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            logger.LogDebug("Sending chat completion request to OpenAI");
            var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            logger.LogInformation("Successfully received response from OpenAI");

            var jsonResponse = response.Value.Content[0].Text;
            logger.LogDebug("Received JSON response: {JsonResponse}", jsonResponse);

            var cleanedJson = CleanJson(jsonResponse);
            logger.LogDebug("Cleaned JSON response: {CleanedJson}", cleanedJson);

            var order = JsonSerializer.Deserialize<Order>(cleanedJson, jsonSerializerOptions);

            if (order == null)
            {
                logger.LogWarning("Deserialization resulted in null order, returning empty order");
                return new Order();
            }

            logger.LogInformation(
                "Successfully extracted order. Device: {Device}, OrderingProvider: {OrderingProvider}",
                order.Device, order.OrderingProvider);

            return order;
        }
        catch (ClientResultException ex)
        {
            logger.LogError(ex, "OpenAI API error occurred during order extraction. Status: {Status}", ex.Status);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize JSON response from OpenAI");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred during order extraction");
            throw;
        }
    }

    private static string CleanJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        var firstBrace = json.IndexOf('{');
        var lastBrace = json.LastIndexOf('}');

        if (firstBrace == -1 || lastBrace == -1 || firstBrace > lastBrace)
            return json;

        return json.Substring(firstBrace, lastBrace - firstBrace + 1);
    }
}