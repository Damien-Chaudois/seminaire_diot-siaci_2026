using System.IO;

namespace wpf.BLL;

public class ImageService : IImageService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];

    public (string Base64, string Extension) LoadImage(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (!((IEnumerable<string>)AllowedExtensions).Contains(ext))
            throw new InvalidOperationException($"Format non supporté : {ext}. Utilisez JPG ou PNG.");

        var bytes = File.ReadAllBytes(filePath);
        var base64 = Convert.ToBase64String(bytes);
        var extension = string.Equals(ext, ".png", StringComparison.Ordinal) ? "png" : "jpeg";

        return (base64, extension);
    }
}
