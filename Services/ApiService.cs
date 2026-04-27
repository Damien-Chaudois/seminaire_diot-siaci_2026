using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Services;

public class ApiService : IApiService
{
    private static readonly HttpClient _httpClient = new();
    private string _apiKey;
    private readonly string _baseUrl;
    private readonly List<string> _availableModels;
    private string _model;

    public ApiService()
    {
        _apiKey = EnvService.Get("GITHUB_PAT");
        _baseUrl = EnvService.Get("API_BASE_URL").TrimEnd('/');

        var configuredModels = ParseModels(EnvService.Get("MODEL_NAMES"));
        var defaultModel = NormalizeModelName(EnvService.Get("MODEL_NAME"));

        _availableModels = BuildModelList(configuredModels, defaultModel);
        _model = _availableModels.FirstOrDefault() ?? string.Empty;
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(_apiKey);
    public string CurrentModel => _model;

    public IReadOnlyList<string> GetAvailableModels()
    {
        return _availableModels.AsReadOnly();
    }

    public void SetModel(string model)
    {
        var normalized = NormalizeModelName(model);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("Le nom du modele ne peut pas etre vide.", nameof(model));

        foreach (var availableModel in _availableModels)
        {
            if (string.Equals(availableModel, normalized, StringComparison.OrdinalIgnoreCase))
            {
                _model = availableModel;
                return;
            }
        }

        throw new ArgumentException($"Le modele '{normalized}' n'est pas present dans MODEL_NAMES.", nameof(model));
    }

    public void SetApiKey(string key) => _apiKey = key;

    public async Task<bool> ValidateApiKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (string.IsNullOrWhiteSpace(_model))
            return false;

        var requestBody = new
        {
            model = _model,
            messages = new[] { new { role = "user", content = "test" } },
            max_tokens = 1
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> SendImageAsync(string base64Image, string extension, string personalityInstruction)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Clé API non configurée. Veuillez renseigner GITHUB_PAT dans le fichier .env.");

        if (string.IsNullOrWhiteSpace(_model))
            throw new InvalidOperationException("Aucun modele configure. Renseignez MODEL_NAMES ou MODEL_NAME dans le fichier .env.");

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
                            text = personalityInstruction
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
        {
            if ((int)response.StatusCode == 401 &&
                responseBody.Contains("models' permission is required", StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpRequestException(
                    "Token GitHub non autorisé pour GitHub Models. " +
                    "Créez un PAT avec la permission 'Models' puis remplacez GITHUB_PAT dans .env.");
            }

            if ((int)response.StatusCode == 400 &&
                responseBody.Contains("unknown_model", StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpRequestException(
                    $"Modele inconnu: '{_model}'. Verifiez MODEL_NAME/MODEL_NAMES dans .env (exemple: gpt-4o)."
                );
            }

            throw new HttpRequestException($"Erreur API ({response.StatusCode}): {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? string.Empty;
    }

    private static List<string> BuildModelList(IEnumerable<string> configuredModels, string defaultModel)
    {
        var models = new List<string>();

        foreach (var model in configuredModels)
        {
            if (models.Any(existing => string.Equals(existing, model, StringComparison.OrdinalIgnoreCase)))
                continue;

            models.Add(model);
        }

        if (!string.IsNullOrWhiteSpace(defaultModel) &&
            !models.Any(existing => string.Equals(existing, defaultModel, StringComparison.OrdinalIgnoreCase)))
        {
            models.Insert(0, defaultModel);
        }

        return models;
    }

    private static IEnumerable<string> ParseModels(string modelsValue)
    {
        if (string.IsNullOrWhiteSpace(modelsValue))
            return [];

        return modelsValue
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeModelName)
            .Where(model => !string.IsNullOrWhiteSpace(model));
    }

    private static string NormalizeModelName(string value)
    {
        var normalized = value.Trim().Trim('"', '\'', '`');

        // Common typos seen in .env values.
        normalized = normalized.Replace("gpt-4.0", "gpt-4o", StringComparison.OrdinalIgnoreCase);
        normalized = normalized.Replace("gpt4o", "gpt-4o", StringComparison.OrdinalIgnoreCase);

        while (normalized.EndsWith("()", StringComparison.Ordinal))
            normalized = normalized[..^2].Trim();

        return normalized;
    }
}
