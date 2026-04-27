using DiceBear.API;
using DiceBear.API.Model;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Web;

namespace Services;

public interface IDiceBearAvatarService
{
    Task<string> GenerateAvatarPngBase64Async(string seed, string? displayName = null, AvaStyle style = AvaStyle.Avataaars, ushort sizePx = 96);
}

public class DiceBearAvatarService : IDiceBearAvatarService
{
    private static readonly HttpClient HttpClient = new();

    public async Task<string> GenerateAvatarPngBase64Async(string seed, string? displayName = null, AvaStyle style = AvaStyle.Avataaars, ushort sizePx = 96)
    {
        if (string.IsNullOrWhiteSpace(seed))
            seed = Guid.NewGuid().ToString("N");

        object sdkResult = await DiceBearAPI.GenerateAndDownloadAva(
            style,
            AvaImageFormat.PNG,
            seed,
            string.Empty,
            false,
            0,
            100,
            50,
            sizePx,
            0x00000000,
            BgColorType.Solid,
            string.Empty,
            0,
            0,
            0,
            true,
            false,
            null);

        var bytes = ExtractBytes(sdkResult);
        if (!LooksLikePng(bytes))
        {
            // Fallback explicite sur le style Personas (plus proche d'un portrait de testeur).
            bytes = await DownloadPersonasAvatarAsync(seed, displayName, sizePx);
        }

        var base64 = Convert.ToBase64String(bytes);
        return base64;
    }

    private static async Task<byte[]> DownloadPersonasAvatarAsync(string seed, string? displayName, ushort sizePx)
    {
        var encodedSeed = HttpUtility.UrlEncode(seed);
        var gender = InferGenderFromName(displayName);

        var query = new StringBuilder();
        query.Append($"seed={encodedSeed}&size={sizePx}&radius=20&backgroundType=gradientLinear");

        switch (gender)
        {
            case GenderHint.Male:
                query.Append("&facialHairProbability=65");
                query.Append("&hair=bald,balding,beanie,buzzcut,cap,curly,fade,mohawk,shortCombover,shortComboverChops,sideShave");
                break;
            case GenderHint.Female:
                query.Append("&facialHairProbability=0");
                query.Append("&hair=bobBangs,bobCut,bunUndercut,curly,curlyBun,extraLong,long,pigtails,straightBun");
                break;
            default:
                query.Append("&facialHairProbability=20");
                break;
        }

        var url = $"https://api.dicebear.com/9.x/personas/png?{query}";
        var bytes = await HttpClient.GetByteArrayAsync(url);

        if (!LooksLikePng(bytes))
            throw new InvalidOperationException("DiceBear n'a pas retourne une image PNG valide.");

        return bytes;
    }

    private static byte[] ExtractBytes(object sdkResult)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var bytes = TryExtractBytesRecursive(sdkResult, visited, 0);
        if (bytes is not null)
            return bytes;

        var typeName = sdkResult.GetType().FullName ?? sdkResult.GetType().Name;
        throw new InvalidOperationException($"Impossible d'extraire les bytes d'avatar depuis le resultat DiceBear SDK. Type recu: {typeName}");
    }

    private static byte[]? TryExtractBytesRecursive(object? candidate, HashSet<object> visited, int depth)
    {
        if (candidate is null)
            return null;

        if (candidate is byte[] rawBytes)
            return rawBytes;

        if (candidate is IEnumerable<byte> byteEnumerable)
            return byteEnumerable.ToArray();

        if (depth > 8)
            return null;

        var type = candidate.GetType();
        if (type.IsPrimitive || type.IsEnum || candidate is string || candidate is decimal || candidate is DateTime)
            return null;

        if (!type.IsValueType)
        {
            if (visited.Contains(candidate))
                return null;

            visited.Add(candidate);
        }

        var flags = System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic;

        foreach (var field in type.GetFields(flags))
        {
            object? value;
            try
            {
                value = field.GetValue(candidate);
            }
            catch
            {
                continue;
            }

            var bytes = TryExtractBytesRecursive(value, visited, depth + 1);
            if (bytes is not null)
                return bytes;
        }

        foreach (var property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            object? value;
            try
            {
                value = property.GetValue(candidate);
            }
            catch
            {
                continue;
            }

            var bytes = TryExtractBytesRecursive(value, visited, depth + 1);
            if (bytes is not null)
                return bytes;
        }

        return null;
    }

    private static bool LooksLikePng(byte[] bytes)
    {
        return bytes.Length > 8 &&
               bytes[0] == 0x89 &&
               bytes[1] == 0x50 &&
               bytes[2] == 0x4E &&
               bytes[3] == 0x47 &&
               bytes[4] == 0x0D &&
               bytes[5] == 0x0A &&
               bytes[6] == 0x1A &&
               bytes[7] == 0x0A;
    }

    private static GenderHint InferGenderFromName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return GenderHint.Unknown;

        var firstToken = displayName
            .Split([' ', '-', '_', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstToken))
            return GenderHint.Unknown;

        var normalized = RemoveDiacritics(firstToken).ToLowerInvariant();

        // Small explicit lists for common French names to reduce suffix ambiguity.
        var knownFemale = new HashSet<string>
        {
            "camille", "sarah", "emma", "lea", "nora", "alice", "chloe", "claire", "julie", "marie", "sophie", "anais"
        };

        var knownMale = new HashSet<string>
        {
            "leo", "yanis", "lucas", "alexandre", "antoine", "thomas", "mathis", "hugo", "nicolas", "pierre", "julien", "kevin"
        };

        if (knownFemale.Contains(normalized))
            return GenderHint.Female;

        if (knownMale.Contains(normalized))
            return GenderHint.Male;

        if (normalized.EndsWith("a") || normalized.EndsWith("ia") || normalized.EndsWith("ine") || normalized.EndsWith("elle"))
            return GenderHint.Female;

        if (normalized.EndsWith("o") || normalized.EndsWith("an") || normalized.EndsWith("on") || normalized.EndsWith("is") || normalized.EndsWith("ien"))
            return GenderHint.Male;

        return GenderHint.Unknown;
    }

    private static string RemoveDiacritics(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private enum GenderHint
    {
        Unknown,
        Male,
        Female
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}