using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FinancialIntelligence.Api.Services.Ai;

public sealed class OpenAiClient : IGenerativeAiClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiSettings _settings;

    public OpenAiClient(
        HttpClient httpClient,
        IOptions<OpenAiSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _settings.Model,
            input = prompt
        };

        var json = JsonSerializer.Serialize(request);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/responses");

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        httpRequest.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI request failed: {response.StatusCode} - {responseContent}");
        }

        using var doc = JsonDocument.Parse(responseContent);

        // Extract text from Responses API
        var output = doc.RootElement
            .GetProperty("output")[0]
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return output ?? string.Empty;
    }
}