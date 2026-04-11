using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FinancialIntelligence.Api.Services;

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

        return await SendRequestAndExtractTextAsync(request, cancellationToken);
    }

    public async Task<string> ChatAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<AiChatMessage>? history = null,
        CancellationToken cancellationToken = default)
    {
        var input = new List<object>();

        if (history != null)
        {
            foreach (var message in history)
            {
                if (string.IsNullOrWhiteSpace(message.Role) ||
                    string.IsNullOrWhiteSpace(message.Content))
                {
                    continue;
                }

                input.Add(new
                {
                    role = NormalizeRole(message.Role),
                    content = new[]
                    {
                        new
                        {
                            type = "input_text",
                            text = message.Content
                        }
                    }
                });
            }
        }

        input.Add(new
        {
            role = "user",
            content = new[]
            {
                new
                {
                    type = "input_text",
                    text = userPrompt
                }
            }
        });

        var request = new
        {
            model = _settings.Model,
            instructions = systemPrompt,
            input = input
        };

        return await SendRequestAndExtractTextAsync(request, cancellationToken);
    }

    private async Task<string> SendRequestAndExtractTextAsync(
        object requestBody,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(requestBody);

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

        return ExtractTextFromResponse(responseContent);
    }

    private static string ExtractTextFromResponse(string responseContent)
    {
        using var doc = JsonDocument.Parse(responseContent);

        if (!doc.RootElement.TryGetProperty("output", out var outputArray) ||
            outputArray.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var outputItem in outputArray.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var contentArray) ||
                contentArray.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in contentArray.EnumerateArray())
            {
                if (!contentItem.TryGetProperty("text", out var textElement))
                {
                    continue;
                }

                var text = textElement.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (sb.Length > 0)
                    {
                        sb.AppendLine();
                    }

                    sb.Append(text);
                }
            }
        }

        return sb.ToString().Trim();
    }

    private static string NormalizeRole(string role)
    {
        return role.Trim().ToLowerInvariant() switch
        {
            "assistant" => "assistant",
            "system" => "system",
            _ => "user"
        };
    }
}

// using System.Net.Http.Headers;
// using System.Text;
// using System.Text.Json;
// using Microsoft.Extensions.Options;

// namespace FinancialIntelligence.Api.Services;

// public sealed class OpenAiClient : IGenerativeAiClient
// {
//     private readonly HttpClient _httpClient;
//     private readonly OpenAiSettings _settings;

//     public OpenAiClient(
//         HttpClient httpClient,
//         IOptions<OpenAiSettings> settings)
//     {
//         _httpClient = httpClient;
//         _settings = settings.Value;
//     }

//     public async Task<string> GenerateAsync(
//         string prompt,
//         CancellationToken cancellationToken = default)
//     {
//         var request = new
//         {
//             model = _settings.Model,
//             input = prompt
//         };

//         var json = JsonSerializer.Serialize(request);

//         using var httpRequest = new HttpRequestMessage(
//             HttpMethod.Post,
//             "https://api.openai.com/v1/responses");

//         httpRequest.Headers.Authorization =
//             new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

//         httpRequest.Content = new StringContent(
//             json,
//             Encoding.UTF8,
//             "application/json");

//         using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

//         var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

//         if (!response.IsSuccessStatusCode)
//         {
//             throw new InvalidOperationException(
//                 $"OpenAI request failed: {response.StatusCode} - {responseContent}");
//         }

//         using var doc = JsonDocument.Parse(responseContent);

//         // Extract text from Responses API
//         var output = doc.RootElement
//             .GetProperty("output")[0]
//             .GetProperty("content")[0]
//             .GetProperty("text")
//             .GetString();

//         return output ?? string.Empty;
//     }
// }