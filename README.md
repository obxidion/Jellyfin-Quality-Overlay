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

Once you have published a release on GitHub (see "Publishing releases" below),
users can install and auto-update the plugin from a repository URL:

1. In Jellyfin go to **Dashboard → Plugins → Repositories → Add**.
2. Set the URL to the hosted manifest:
   `https://raw.githubusercontent.com/obxidion/Jellyfin-Quality-Overlay/main/manifest.json`
3. Open **Dashboard → Plugins → Catalog**, install **Quality Overlay**, and
   restart Jellyfin when prompted.

## Publishing releases (maintainers)

Releases are fully automated by `.github/workflows/release.yml`. On every tag
that starts with `v`, the workflow builds the plugin, attaches a versioned zip to
a GitHub Release, and updates `manifest.json` on the default branch so the
repository URL above always points at the latest build.

One-time setup:

1. Create a GitHub repository from this folder and push it.
2. Set `owner` in `build.yaml` to your GitHub username or org.

Cut a release:

```bash
git tag v1.0.0.0
git push origin v1.0.0.0
```

Use a four-part version (`MAJOR.MINOR.PATCH.BUILD`) that matches `version` in
`build.yaml`. The workflow handles packaging, the GitHub Release, the checksum,
and the manifest update. No manual editing of `manifest.json` is required — it
ships as an empty list and is populated automatically with each release.

### Manual repository hosting (optional)

If you prefer not to use GitHub Actions, you can build a versioned zip locally
with `jprm plugin build .`, host it anywhere, and maintain `manifest.json` by
hand using `jprm repo add --url <release-base-url> ./ <plugin-zip>`.

## Requirements

- Jellyfin 10.11 or newer
- .NET 9.0 runtime (bundled with Jellyfin 10.11+)

## Dependencies

- `Jellyfin.Controller`, `Jellyfin.Model` — referenced with `ExcludeAssets=runtime`
  (provided by the server at runtime)
- `SkiaSharp` — referenced with `ExcludeAssets=runtime` (the native SkiaSharp
  library ships with Jellyfin 10.11+)
