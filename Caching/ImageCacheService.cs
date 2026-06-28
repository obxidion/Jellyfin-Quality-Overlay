using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Jellyfin.Plugin.QualityOverlay.Configuration;
using Jellyfin.Plugin.QualityOverlay.Detection;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.QualityOverlay.Caching;

public class ImageCacheService
{
    private readonly string _cacheRoot;
    private readonly ILogger<ImageCacheService> _logger;

    public ImageCacheService(IApplicationPaths applicationPaths, ILogger<ImageCacheService> logger)
    {
        _logger = logger;
        _cacheRoot = Path.Combine(applicationPaths.CachePath, "qualityoverlay");
        Directory.CreateDirectory(_cacheRoot);
    }

    public string BuildKey(
        Guid itemId,
        string imageType,
        string? imageIndex,
        string? queryString,
        long versionTicks,
        IReadOnlyList<BadgeLabel> labels,
        PluginConfiguration config)
    {
        var builder = new StringBuilder();
        builder.Append(itemId.ToString("N"));
        builder.Append('|').Append(imageType);
        builder.Append('|').Append(imageIndex ?? "0");
        builder.Append('|').Append(queryString ?? string.Empty);
        builder.Append('|').Append(versionTicks.ToString(CultureInfo.InvariantCulture));

        foreach (var label in labels)
        {
            builder.Append('|').Append(label.Text);
        }

        builder.Append('|').Append(config.Position);
        builder.Append('|').Append(config.BadgeScale.ToString(CultureInfo.InvariantCulture));
        builder.Append('|').Append(config.Margin);
        builder.Append('|').Append(config.BackgroundOpacity.ToString(CultureInfo.InvariantCulture));
        builder.Append('|').Append(config.BackgroundColor);
        builder.Append('|').Append(config.TextColor);
        builder.Append('|').Append(config.VideoAccentColor);
        builder.Append('|').Append(config.AudioAccentColor);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes);
    }

    public CachedImage? Get(string key, PluginConfiguration config)
    {
        try
        {
            var path = GetPath(key);
            if (!File.Exists(path))
            {
                return null;
            }

            if (config.CacheExpirationHours > 0)
            {
                var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(path);
                if (age.TotalHours > config.CacheExpirationHours)
                {
                    File.Delete(path);
                    return null;
                }
            }

            var bytes = File.ReadAllBytes(path);
            var contentType = path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
            return new CachedImage(bytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Quality Overlay cache read failed for {Key}", key);
            return null;
        }
    }

    public void Set(string key, byte[] data, string contentType)
    {
        try
        {
            var path = GetPath(key, contentType);
            var temp = path + ".tmp";
            File.WriteAllBytes(temp, data);
            File.Move(temp, path, true);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Quality Overlay cache write failed for {Key}", key);
        }
    }

    private string GetPath(string key, string? contentType = null)
    {
        var extension = contentType is not null && contentType.Contains("png", StringComparison.OrdinalIgnoreCase)
            ? ".png"
            : ".jpg";

        if (contentType is null)
        {
            var pngPath = Path.Combine(_cacheRoot, key + ".png");
            return File.Exists(pngPath) ? pngPath : Path.Combine(_cacheRoot, key + ".jpg");
        }

        return Path.Combine(_cacheRoot, key + extension);
    }
}

public readonly record struct CachedImage(byte[] Bytes, string ContentType);
