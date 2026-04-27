namespace Services;

public interface IApiService
{
    bool HasApiKey { get; }
    string CurrentApiKey { get; }
    string CurrentModel { get; }
    IReadOnlyList<string> GetAvailableModels();
    Task<string> SendImageAsync(string base64Image, string extension, string personalityInstruction);
    Task<string> SendTextAsync(string prompt);
    void SetModel(string model);
    void SetApiKey(string key);
    Task<bool> ValidateApiKeyAsync(string key);
}
