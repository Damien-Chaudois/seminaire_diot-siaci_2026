namespace wpf.BLL;

public interface IImageService
{
    /// <summary>Converts an image file to a base64 string and returns extension (jpeg/png).</summary>
    (string Base64, string Extension) LoadImage(string filePath);
}
