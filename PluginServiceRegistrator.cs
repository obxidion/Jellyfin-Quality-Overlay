using Jellyfin.Plugin.QualityOverlay.Caching;
using Jellyfin.Plugin.QualityOverlay.Detection;
using Jellyfin.Plugin.QualityOverlay.Drawing;
using Jellyfin.Plugin.QualityOverlay.Startup;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.QualityOverlay;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<MediaInfoResolver>();
        serviceCollection.AddSingleton<BadgeRenderer>();
        serviceCollection.AddSingleton<ImageCacheService>();
        serviceCollection.AddSingleton<IStartupFilter, QualityOverlayStartupFilter>();
    }
}
