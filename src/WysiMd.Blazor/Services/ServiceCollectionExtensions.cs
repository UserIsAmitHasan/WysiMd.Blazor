using WysiMd.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace WysiMd.Blazor;

/// <summary>
/// Extension methods for registering WysiMd.Blazor services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the WysiMd.Blazor services to the DI container.
    /// Call this in your Program.cs: builder.Services.AddWysiMdBlazor();
    /// </summary>
    public static IServiceCollection AddWysiMdBlazor(
        this IServiceCollection services)
    {
        services.AddSingleton<MarkdownService>();
        return services;
    }
}
