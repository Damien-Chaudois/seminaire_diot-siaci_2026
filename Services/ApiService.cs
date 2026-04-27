using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Services;

public class ApiService : IApiService
{
    private static readonly HttpClient _httpClient = new();
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;

    public ApiService()
    {
        _apiKey = EnvService.Get("GITHUB_PAT");
        _baseUrl = EnvService.Get("API_BASE_URL").TrimEnd('/');
        _model = EnvService.Get("MODEL_NAME");

        if (string.IsNullOrWhiteSpace(_model))
            _model = "gpt-4o";
    }

    public async Task<string> SendImageAsync(string base64Image, string extension, string prompt)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Clé API non configurée. Veuillez renseigner GITHUB_PAT dans le fichier .env.");

        var mimeType = extension == "png" ? "image/png" : "image/jpeg";

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:{mimeType};base64,{base64Image}" }
                        },
                        new
                        {
                            type = "text",
                            text = string.IsNullOrWhiteSpace(prompt) ? "Décris cette image." : prompt
                        }
                    }
                }
            },
            max_tokens = 1024
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Erreur API ({response.StatusCode}): {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? string.Empty;
    }
}
