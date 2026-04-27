namespace BLL;

public interface ILlmService
{
    Task<string> AnalyzeImageAsync(string base64Image, string extension, string prompt);
}
