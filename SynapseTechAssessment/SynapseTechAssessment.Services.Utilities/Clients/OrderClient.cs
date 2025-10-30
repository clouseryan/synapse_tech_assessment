using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SynapseTechAssessment.Data.Models;

namespace SynapseTechAssessment.Services.Utilities.Clients;

public class OrderClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderClient> _logger;

    public OrderClient(HttpClient httpClient, ILogger<OrderClient> logger, IOptionsSnapshot<HttpClientSettings> optionsSettings)
    {
        _httpClient = httpClient;
        _logger = logger;

        var settings = optionsSettings.Get("OrderClientSettings");
        _httpClient.BaseAddress = new Uri(settings.Host);
        _httpClient.Timeout = TimeSpan.FromSeconds(settings.ClientTimeout);

    }

    public async Task PostOrderAsync(Order order, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting PostOrderAsync for Order ID: {OrderId}", order.Device);

        try
        {
            var jsonContent = JsonSerializer.Serialize(order);
            _logger.LogDebug("Serialized order payload: {Payload}", jsonContent);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Posting order for {Device} to endpoint", order.Device);
            var response = await _httpClient.PostAsync("/orders", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully posted order {OrderId}. Status: {StatusCode}",
                    order.Device, response.StatusCode);
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to post order {OrderId}. Status: {StatusCode}, Response: {Response}",
                    order.Device, response.StatusCode, responseBody);
                response.EnsureSuccessStatusCode();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception occurred while posting order {OrderId}", order.Device);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout or cancellation while posting order {OrderId}", order.Device);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while posting order {OrderId}", order.Device);
            throw;
        }
    }
}