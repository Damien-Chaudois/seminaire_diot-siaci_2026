using System.IO;

namespace Services;

/// <summary>Reads key=value pairs from a .env file.</summary>
public static class EnvService
{
    private static readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public static void Load(string envFilePath)
    {
        if (!File.Exists(envFilePath))
            return;

        foreach (var line in File.ReadAllLines(envFilePath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var idx = trimmed.IndexOf('=');
            if (idx <= 0) continue;

            var key = trimmed[..idx].Trim();
            var value = trimmed[(idx + 1)..].Trim();
            _values[key] = value;
        }
    }

    public static string Get(string key)
    {
        // Chercher d'abord dans le dictionnaire interne
        if (_values.TryGetValue(key, out var value))
            return value;
        
        // Fallback vers les variables d'environnement système
        return Environment.GetEnvironmentVariable(key) ?? string.Empty;
    }
}
