namespace Services;

public interface IApiService
{
    bool HasApiKey { get; }
    string CurrentModel { get; }
    IReadOnlyList<string> GetAvailableModels();
    Task<string> SendImageAsync(string base64Image, string extension, string personalityInstruction);
    void SetModel(string model);
    void SetApiKey(string key);
    Task<bool> ValidateApiKeyAsync(string key);
}
