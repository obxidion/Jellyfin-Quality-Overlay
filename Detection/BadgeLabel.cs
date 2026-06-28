namespace Jellyfin.Plugin.QualityOverlay.Detection;

public enum BadgeKind
{
    Video,
    Audio
}

public readonly record struct BadgeLabel(string Text, BadgeKind Kind);
