namespace BLL;

public interface IImageService
{
    (string Base64, string Extension) LoadImage(string filePath);
}
