using Jellyfin.Plugin.QualityOverlay.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Jellyfin.Plugin.QualityOverlay.Startup;

public class QualityOverlayStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            builder.UseMiddleware<QualityOverlayMiddleware>();
            next(builder);
        };
    }
}
