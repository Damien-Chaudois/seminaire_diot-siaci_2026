using Services;

namespace BLL;

public class LlmService : ILlmService
{
    private readonly IApiService _apiService;

    public LlmService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public Task<string> AnalyzeImageAsync(string base64Image, string extension, string personalityInstruction)
    {
        return _apiService.SendImageAsync(base64Image, extension, personalityInstruction);
    }
}
