using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nergard.Opti.AutoRegister.Internal;
using Nergard.Opti.AutoRegister.Resolution;

namespace Nergard.Opti.AutoRegister;

/// <summary>
/// Registration helpers for the AutoRegister feature.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AutoRegister services. Called automatically by the initialization module, so a
    /// plain NuGet install needs no Startup code. Call it explicitly only to supply options.
    /// </summary>
    /// <remarks>
    /// Uses <c>TryAdd</c> so a custom <see cref="ISettingsTargetResolver"/> registered earlier wins.
    /// To override the default resolver regardless of order, use
    /// <c>services.Replace(ServiceDescriptor.Transient&lt;ISettingsTargetResolver, MyResolver&gt;())</c>.
    /// </remarks>
    public static IServiceCollection AddNergardAutoRegister(
        this IServiceCollection services,
        Action<AutoRegisterOptions>? configure = null)
    {
        var options = new AutoRegisterOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<PropertyMatcher>();
        services.TryAddTransient<ISettingsTargetResolver, ContentTreeSettingsTargetResolver>();
        services.TryAddTransient<IContentRegistrar, ContentRegistrar>();

        return services;
    }
}
