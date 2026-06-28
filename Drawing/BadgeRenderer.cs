using Jellyfin.Plugin.QualityOverlay.Configuration;
using Jellyfin.Plugin.QualityOverlay.Detection;
using SkiaSharp;

namespace Jellyfin.Plugin.QualityOverlay.Drawing;

public class BadgeRenderer
{
    public byte[]? Render(byte[] source, string contentType, IReadOnlyList<BadgeLabel> labels, PluginConfiguration config)
    {
        if (labels.Count == 0)
        {
            return null;
        }

        using var bitmap = SKBitmap.Decode(source);
        if (bitmap is null)
        {
            return null;
        }

        using var canvas = new SKCanvas(bitmap);

        var shortEdge = Math.Min(bitmap.Width, bitmap.Height);
        var fontSize = Clamp((float)(shortEdge * 0.052 * config.BadgeScale), 13f, 64f);
        var paddingX = fontSize * 0.62f;
        var paddingY = fontSize * 0.34f;
        var gap = fontSize * 0.42f;
        var margin = (float)config.Margin;

        using var typeface = SKTypeface.FromFamilyName(null, SKFontStyle.Bold)
            ?? SKTypeface.Default;
        using var font = new SKFont(typeface, fontSize) { Edging = SKFontEdging.SubpixelAntialias };

        using var textPaint = new SKPaint { IsAntialias = true, Color = ParseColor(config.TextColor, SKColors.White) };
        using var bgPaint = new SKPaint { IsAntialias = true };
        using var accentPaint = new SKPaint { IsAntialias = true };

        var background = ParseColor(config.BackgroundColor, new SKColor(16, 16, 16))
            .WithAlpha((byte)(Clamp((float)config.BackgroundOpacity, 0f, 1f) * 255));

        var badges = new List<RenderedBadge>(labels.Count);
        var fontMetrics = font.Metrics;
        var textHeight = fontMetrics.Descent - fontMetrics.Ascent;
        var badgeHeight = textHeight + (paddingY * 2);
        var accentWidth = fontSize * 0.18f;

        foreach (var label in labels)
        {
            var textWidth = font.MeasureText(label.Text);
            var badgeWidth = textWidth + (paddingX * 2) + accentWidth + (paddingX * 0.5f);
            var accent = label.Kind == BadgeKind.Video
                ? ParseColor(config.VideoAccentColor, new SKColor(63, 167, 255))
                : ParseColor(config.AudioAccentColor, new SKColor(242, 183, 5));
            badges.Add(new RenderedBadge(label.Text, badgeWidth, accent));
        }

        var totalHeight = (badgeHeight * badges.Count) + (gap * (badges.Count - 1));
        var isBottom = config.Position is BadgePosition.BottomLeft or BadgePosition.BottomRight;
        var isRight = config.Position is BadgePosition.TopRight or BadgePosition.BottomRight;

        var currentY = isBottom
            ? bitmap.Height - margin - totalHeight
            : margin;

        var radius = badgeHeight / 2f;

        foreach (var badge in badges)
        {
            var x = isRight ? bitmap.Width - margin - badge.Width : margin;
            var rect = new SKRect(x, currentY, x + badge.Width, currentY + badgeHeight);

            bgPaint.Color = background;
            canvas.DrawRoundRect(rect, radius, radius, bgPaint);

            accentPaint.Color = badge.Accent;
            var accentRect = new SKRect(
                rect.Left + paddingX,
                rect.MidY - (badgeHeight * 0.28f),
                rect.Left + paddingX + accentWidth,
                rect.MidY + (badgeHeight * 0.28f));
            canvas.DrawRoundRect(accentRect, accentWidth / 2f, accentWidth / 2f, accentPaint);

            var baseline = rect.MidY - ((fontMetrics.Ascent + fontMetrics.Descent) / 2f);
            var textX = accentRect.Right + (paddingX * 0.5f);
            canvas.DrawText(badge.Text, textX, baseline, SKTextAlign.Left, font, textPaint);

            currentY += badgeHeight + gap;
        }

        canvas.Flush();

        var format = contentType.Contains("png", StringComparison.OrdinalIgnoreCase)
            ? SKEncodedImageFormat.Png
            : SKEncodedImageFormat.Jpeg;

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 92);
        return data?.ToArray();
    }

    private static float Clamp(float value, float min, float max) => Math.Min(Math.Max(value, min), max);

    private static SKColor ParseColor(string value, SKColor fallback)
    {
        return SKColor.TryParse(value, out var color) ? color : fallback;
    }

    private readonly record struct RenderedBadge(string Text, float Width, SKColor Accent);
}
