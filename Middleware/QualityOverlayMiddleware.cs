using System.Text.RegularExpressions;
using Jellyfin.Plugin.QualityOverlay.Caching;
using Jellyfin.Plugin.QualityOverlay.Configuration;
using Jellyfin.Plugin.QualityOverlay.Detection;
using Jellyfin.Plugin.QualityOverlay.Drawing;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.QualityOverlay.Middleware;

public partial class QualityOverlayMiddleware
{
    private const long MaxImageBytes = 25 * 1024 * 1024;

    private readonly RequestDelegate _next;
    private readonly ILibraryManager _libraryManager;
    private readonly MediaInfoResolver _resolver;
    private readonly BadgeRenderer _renderer;
    private readonly ImageCacheService _cache;
    private readonly ILogger<QualityOverlayMiddleware> _logger;

    public QualityOverlayMiddleware(
        RequestDelegate next,
        ILibraryManager libraryManager,
        MediaInfoResolver resolver,
        BadgeRenderer renderer,
        ImageCacheService cache,
        ILogger<QualityOverlayMiddleware> logger)
    {
        _next = next;
        _libraryManager = libraryManager;
        _resolver = resolver;
        _renderer = renderer;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!TryParseRequest(context, out var request))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var config = Plugin.Instance?.Configuration;
        if (config is null || !ShouldProcess(config, request.ImageType))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var item = _libraryManager.GetItemById(request.ItemId);
        if (item is null)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        IReadOnlyList<BadgeLabel> labels;
        try
        {
            labels = _resolver.GetLabels(request.ItemId, config);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Quality Overlay failed to resolve media info for {ItemId}", request.ItemId);
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (labels.Count == 0)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var versionTicks = item.DateModified.ToUniversalTime().Ticks;
        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null;
        var cacheKey = _cache.BuildKey(request.ItemId, request.ImageType, request.ImageIndex, queryString, versionTicks, labels, config);

        var cached = _cache.Get(cacheKey, config);
        if (cached is not null)
        {
            await WriteImageAsync(context, cached.Value.Bytes, cached.Value.ContentType).ConfigureAwait(false);
            return;
        }

        // Force a full 200 response from the image endpoint so we always have
        // bytes to overlay. Without this, a browser-cached original would trigger
        // a 304 (no body) and the un-overlaid cached image would be shown.
        context.Request.Headers.Remove(HeaderNames.IfNoneMatch);
        context.Request.Headers.Remove(HeaderNames.IfModifiedSince);
        context.Request.Headers.Remove(HeaderNames.IfRange);
        context.Request.Headers.Remove(HeaderNames.Range);

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        var contentType = context.Response.ContentType ?? string.Empty;
        if (context.Response.StatusCode != StatusCodes.Status200OK
            || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || buffer.Length == 0
            || buffer.Length > MaxImageBytes)
        {
            await FlushOriginalAsync(context, buffer, originalBody).ConfigureAwait(false);
            return;
        }

        var source = buffer.ToArray();
        byte[]? processed = null;
        try
        {
            processed = _renderer.Render(source, contentType, labels, config);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Quality Overlay rendering failed for {ItemId}", request.ItemId);
        }

        if (processed is null)
        {
            await FlushOriginalAsync(context, buffer, originalBody).ConfigureAwait(false);
            return;
        }

        _cache.Set(cacheKey, processed, contentType);

        context.Response.ContentType = contentType;
        context.Response.ContentLength = processed.Length;
        await originalBody.WriteAsync(processed).ConfigureAwait(false);
    }

    private static async Task FlushOriginalAsync(HttpContext context, MemoryStream buffer, Stream originalBody)
    {
        buffer.Position = 0;
        if (buffer.Length > 0)
        {
            context.Response.ContentLength = buffer.Length;
            await buffer.CopyToAsync(originalBody).ConfigureAwait(false);
        }
    }

    private static async Task WriteImageAsync(HttpContext context, byte[] bytes, string contentType)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = contentType;
        context.Response.ContentLength = bytes.Length;
        await context.Response.Body.WriteAsync(bytes).ConfigureAwait(false);
    }

    private static bool ShouldProcess(PluginConfiguration config, string imageType)
    {
        return imageType.ToLowerInvariant() switch
        {
            "primary" => config.ProcessPrimary,
            "thumb" => config.ProcessThumb,
            "backdrop" => config.ProcessBackdrop,
            _ => false
        };
    }

    private static bool TryParseRequest(HttpContext context, out ImageRequest request)
    {
        request = default;

        if (!HttpMethods.IsGet(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var match = ImageRequestRegex().Match(path);
        if (!match.Success)
        {
            return false;
        }

        if (!Guid.TryParse(match.Groups["id"].Value, out var itemId))
        {
            return false;
        }

        var index = match.Groups["index"].Success ? match.Groups["index"].Value : null;
        request = new ImageRequest(itemId, match.Groups["type"].Value, index);
        return true;
    }

    [GeneratedRegex(@"^/Items/(?<id>[0-9a-fA-F]{8}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{12})/Images/(?<type>[A-Za-z]+)(?:/(?<index>\d+))?", RegexOptions.IgnoreCase)]
    private static partial Regex ImageRequestRegex();

    private readonly record struct ImageRequest(Guid ItemId, string ImageType, string? ImageIndex);
}
