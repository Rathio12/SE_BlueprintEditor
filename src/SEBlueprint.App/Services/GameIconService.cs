using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BCnEncoder.Decoder;
using SEBlueprint.Core;

namespace SEBlueprint.App.Services;

/// <summary>
/// Shows the game's own block and item icons by decoding the .dds files straight from the user's local
/// Space Engineers install (or mod folder). Nothing is copied or bundled; failures just return null.
/// </summary>
public sealed class GameIconService
{
    readonly ConcurrentDictionary<string, ImageSource?> _cache = new(StringComparer.OrdinalIgnoreCase);
    readonly BcDecoder _decoder = new();

    public string? ContentDir { get; set; }
    public string? WorkshopDir { get; set; }

    /// <param name="iconPath">Path from the definition, e.g. "Textures/GUI/Icons/component/steel_plate_component.dds".</param>
    /// <param name="modId">Mod that defines the item, or null for vanilla.</param>
    public ImageSource? Get(string? iconPath, string? modId = null)
    {
        if (string.IsNullOrWhiteSpace(iconPath)) return null;
        var file = Resolve(iconPath, modId);
        if (file == null) return null;
        return _cache.GetOrAdd(file, Decode);
    }

    string? Resolve(string iconPath, string? modId)
    {
        try
        {
            var rel = iconPath.Replace('/', Path.DirectorySeparatorChar);
            if (modId != null && WorkshopDir != null)
            {
                var modFile = Path.Combine(WorkshopDir, modId, rel);
                if (File.Exists(modFile)) return modFile;
            }
            if (ContentDir != null)
            {
                var vanilla = Path.Combine(ContentDir, rel);
                if (File.Exists(vanilla)) return vanilla;
            }
        }
        catch (Exception) { }
        return null;
    }

    ImageSource? Decode(string file)
    {
        try
        {
            if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(file);
                bmp.DecodePixelWidth = 64;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }

            using var fs = File.OpenRead(file);
            var image = _decoder.Decode2D(fs);
            int w = image.Width, h = image.Height;
            var pixels = new byte[w * h * 4];
            var span = image.Span;
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var c = span[y, x];
                    var i = (y * w + x) * 4;
                    pixels[i] = c.b;
                    pixels[i + 1] = c.g;
                    pixels[i + 2] = c.r;
                    pixels[i + 3] = c.a;
                }
            var source = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, pixels, w * 4);
            source.Freeze();
            return source;
        }
        catch (Exception ex)
        {
            Log.Write($"Icon not decodable {file}: {ex.Message}");
            return null;
        }
    }
}
