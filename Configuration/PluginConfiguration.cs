using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.QualityOverlay.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool EnableVideoQualityBadge { get; set; } = true;

    public bool EnableAudioCodecBadge { get; set; } = true;

    public BadgePosition Position { get; set; } = BadgePosition.BottomRight;

    public double BadgeScale { get; set; } = 1.0;

    public int Margin { get; set; } = 18;

    public double BackgroundOpacity { get; set; } = 0.72;

    public string BackgroundColor { get; set; } = "#101010";

    public string TextColor { get; set; } = "#FFFFFF";

    public string VideoAccentColor { get; set; } = "#3FA7FF";

    public string AudioAccentColor { get; set; } = "#F2B705";

    public bool ProcessPrimary { get; set; } = true;

    public bool ProcessThumb { get; set; } = true;

    public bool ProcessBackdrop { get; set; }

    public int CacheExpirationHours { get; set; } = 168;
}
