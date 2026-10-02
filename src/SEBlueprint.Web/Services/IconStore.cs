using BCnEncoder.Decoder;
using Microsoft.AspNetCore.Components.Forms;
using SEBlueprint.Core.Imaging;

namespace SEBlueprint.Web.Services;

public sealed class IconStore
{
    const long MaxIconBytes = 8 * 1024 * 1024;
    readonly Dictionary<string, IBrowserFile> _files = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Task<string?>> _cache = new(StringComparer.OrdinalIgnoreCase);
    readonly BcDecoder _decoder = new();

    public int Count => _files.Count;

    public static string? KeyFromPath(string path, string? modId)
    {
        var p = path.Replace('\\', '/');
        var i = p.IndexOf("Textures/", StringComparison.OrdinalIgnoreCase);
        if (i < 0 || !p.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)) return null;
        var rel = p[i..];
        return modId == null ? rel : $"{modId}|{rel}";
    }

    public void Add(string key, IBrowserFile file)
    {
        _files[key] = file;
        _cache.Remove(key);
    }

    public Task<string?> GetAsync(string? iconPath, string? modId)
    {
        if (string.IsNullOrWhiteSpace(iconPath)) return Task.FromResult<string?>(null);
        var rel = iconPath.Replace('\\', '/');
        var key = modId != null && _files.ContainsKey($"{modId}|{rel}") ? $"{modId}|{rel}" : rel;
        if (!_files.TryGetValue(key, out var file)) return Task.FromResult<string?>(null);
        if (!_cache.TryGetValue(key, out var task))
            _cache[key] = task = DecodeAsync(file);
        return task;
    }

    async Task<string?> DecodeAsync(IBrowserFile file)
    {
        try
        {
            using var ms = new MemoryStream();
            await file.OpenReadStream(MaxIconBytes).CopyToAsync(ms);
            ms.Position = 0;
            var image = await _decoder.Decode2DAsync(ms);
            var rgba = ToRgba(image, out var w, out var h);
            return "data:image/png;base64," + Convert.ToBase64String(Png.Encode(w, h, rgba));
        }
        catch (Exception)
        {
            return null;
        }
    }

    static byte[] ToRgba(CommunityToolkit.HighPerformance.Memory2D<BCnEncoder.Shared.ColorRgba32> image, out int w, out int h)
    {
        w = image.Width;
        h = image.Height;
        var rgba = new byte[w * h * 4];
        var span = image.Span;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var c = span[y, x];
                var i = (y * w + x) * 4;
                rgba[i] = c.r;
                rgba[i + 1] = c.g;
                rgba[i + 2] = c.b;
                rgba[i + 3] = c.a;
            }
        return rgba;
    }
}
