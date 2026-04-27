using DiceBear.API;
using DiceBear.API.Model;

namespace Services;

public interface IDiceBearAvatarService
{
    Task<string> GenerateAvatarPngBase64Async(string seed, AvaStyle style = AvaStyle.AdventurerNeutral, ushort sizePx = 96);
}

public class DiceBearAvatarService : IDiceBearAvatarService
{
    public async Task<string> GenerateAvatarPngBase64Async(string seed, AvaStyle style = AvaStyle.AdventurerNeutral, ushort sizePx = 96)
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
        var base64 = Convert.ToBase64String(bytes);
        return base64;
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