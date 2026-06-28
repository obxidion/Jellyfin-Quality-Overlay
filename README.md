# Quality Overlay

A Jellyfin plugin that overlays clean video quality and audio codec badges onto
poster, thumbnail, and backdrop images. Badges are drawn in memory while images
are served — original image files are never modified.

- Video quality: 4K, 1440p, 1080p, 720p, 480p, SD (from the highest quality video stream)
- Audio codec: best/main audio stream with channels, e.g. `DTS-HD MA 7.1`,
  `TrueHD Atmos`, `DD+ 5.1`, `AAC 5.1`, `DTS 5.1`, `FLAC 2.0`

## Configuration

Open **Dashboard → Plugins → Quality Overlay** to configure:

- Toggle the video quality badge and audio codec badge independently
- Choose which image types to process (Primary/Poster, Thumbnail, Backdrop)
- Badge position (any corner)
- Badge size, margin, background opacity, background/text colors, and accent colors

Changes take effect immediately. Processed images are cached on disk with a
configurable expiration. The cache is keyed on the source image version, so it is
refreshed automatically when the underlying image changes.

## Build

Requires the .NET 9.0 SDK.

```bash
dotnet restore
dotnet publish -c Release -o ./publish
```

The compiled plugin is `./publish/Jellyfin.Plugin.QualityOverlay.dll`.

### Optional: package with jprm

[`jprm`](https://github.com/oddstr13/jellyfin-plugin-repository-manager) produces a
versioned plugin zip from `build.yaml`:

```bash
pip install jprm
jprm plugin build .
```

## Install (manual)

1. Build the plugin (see above).
2. On your Jellyfin server, create a folder:
   `<jellyfin-config>/plugins/QualityOverlay`
   (commonly `/config/plugins/QualityOverlay` for Docker installs).
3. Copy `Jellyfin.Plugin.QualityOverlay.dll` into that folder.
4. Restart Jellyfin.
5. Confirm it loaded under **Dashboard → Plugins**, then configure it.

## Install via the Jellyfin plugin repository (GitHub)

Users can install and auto-update the plugin from a repository URL:

1. In Jellyfin go to **Dashboard → Plugins → Repositories → Add**.
2. Set the URL to your hosted manifest:
   `https://raw.githubusercontent.com/obxidion/Jellyfin-Quality-Overlay/main/manifest.json`.
3. Open **Dashboard → Plugins → Catalog**, install **Quality Overlay**, and
   restart Jellyfin when prompted.


## Requirements

- Jellyfin 10.11 or newer
- .NET 9.0 runtime (bundled with Jellyfin 10.11+)

## Dependencies

- `Jellyfin.Controller`, `Jellyfin.Model` — referenced with `ExcludeAssets=runtime`
  (provided by the server at runtime)
- `SkiaSharp` — referenced with `ExcludeAssets=runtime` (the native SkiaSharp
  library ships with Jellyfin 10.11+)
