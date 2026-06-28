using Jellyfin.Plugin.QualityOverlay.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.QualityOverlay.Detection;

public class MediaInfoResolver
{
    private readonly IMediaSourceManager _mediaSourceManager;

    public MediaInfoResolver(IMediaSourceManager mediaSourceManager)
    {
        _mediaSourceManager = mediaSourceManager;
    }

    public IReadOnlyList<BadgeLabel> GetLabels(Guid itemId, PluginConfiguration config)
    {
        var streams = _mediaSourceManager.GetMediaStreams(itemId);
        if (streams is null || streams.Count == 0)
        {
            return Array.Empty<BadgeLabel>();
        }

        var labels = new List<BadgeLabel>(2);

        if (config.EnableVideoQualityBadge)
        {
            var quality = ResolveVideoQuality(streams);
            if (quality is not null)
            {
                labels.Add(new BadgeLabel(quality, BadgeKind.Video));
            }
        }

        if (config.EnableAudioCodecBadge)
        {
            var audio = ResolveAudio(streams);
            if (audio is not null)
            {
                labels.Add(new BadgeLabel(audio, BadgeKind.Audio));
            }
        }

        return labels;
    }

    private static string? ResolveVideoQuality(IReadOnlyList<MediaStream> streams)
    {
        var video = streams
            .Where(s => s.Type == MediaStreamType.Video && !s.IsExternal)
            .OrderByDescending(s => (long)(s.Width ?? 0) * (s.Height ?? 0))
            .FirstOrDefault();

        if (video is null)
        {
            return null;
        }

        var width = video.Width ?? 0;
        var height = video.Height ?? 0;
        if (width == 0 && height == 0)
        {
            return null;
        }

        if (width >= 3500 || height >= 2000)
        {
            return "4K";
        }

        if (width >= 2400 || height >= 1300)
        {
            return "1440p";
        }

        if (width >= 1800 || height >= 1000)
        {
            return "1080p";
        }

        if (width >= 1200 || height >= 700)
        {
            return "720p";
        }

        if (width >= 700 || height >= 460)
        {
            return "480p";
        }

        return "SD";
    }

    private static string? ResolveAudio(IReadOnlyList<MediaStream> streams)
    {
        var audio = streams
            .Where(s => s.Type == MediaStreamType.Audio)
            .OrderByDescending(s => s.IsDefault)
            .ThenByDescending(s => s.Channels ?? 0)
            .ThenByDescending(s => s.BitRate ?? 0)
            .FirstOrDefault();

        if (audio is null)
        {
            return null;
        }

        var codec = FormatCodec(audio);
        var channels = FormatChannels(audio.Channels);

        if (string.IsNullOrEmpty(codec))
        {
            return channels;
        }

        return string.IsNullOrEmpty(channels) ? codec : $"{codec} {channels}";
    }

    private static string FormatCodec(MediaStream audio)
    {
        var codec = (audio.Codec ?? string.Empty).ToLowerInvariant();
        var profile = (audio.Profile ?? string.Empty).ToLowerInvariant();
        var title = (audio.Title ?? string.Empty).ToLowerInvariant();
        var atmos = profile.Contains("atmos") || title.Contains("atmos");

        switch (codec)
        {
            case "truehd":
                return atmos ? "TrueHD Atmos" : "TrueHD";
            case "eac3":
                return atmos ? "DD+ Atmos" : "DD+";
            case "ac3":
                return "DD";
            case "dts":
                if (profile.Contains("ma"))
                {
                    return "DTS-HD MA";
                }

                if (profile.Contains("hra") || profile.Contains("hr"))
                {
                    return "DTS-HD HR";
                }

                if (profile.Contains("x") || title.Contains("dts:x"))
                {
                    return "DTS:X";
                }

                return "DTS";
            case "aac":
                return "AAC";
            case "flac":
                return "FLAC";
            case "opus":
                return "Opus";
            case "vorbis":
                return "Vorbis";
            case "mp3":
                return "MP3";
            case "pcm_s16le":
            case "pcm_s24le":
            case "pcm_s32le":
                return "PCM";
            default:
                return string.IsNullOrEmpty(codec) ? string.Empty : codec.ToUpperInvariant();
        }
    }

    private static string FormatChannels(int? channels)
    {
        return channels switch
        {
            null or 0 => string.Empty,
            1 => "Mono",
            2 => "2.0",
            3 => "2.1",
            4 => "4.0",
            5 => "4.1",
            6 => "5.1",
            7 => "6.1",
            8 => "7.1",
            _ => $"{channels}ch"
        };
    }
}
