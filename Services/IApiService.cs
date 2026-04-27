namespace Services;

public interface IApiService
{
    Task<string> SendImageAsync(string base64Image, string extension, string prompt);
}
