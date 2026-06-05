using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Core;
using Microsoft.Extensions.Logging;

namespace Nergard.Opti.AutoRegister.Resolution;

/// <summary>
/// Default <see cref="ISettingsTargetResolver"/>. Resolves the settings instance from the content
/// tree, scoped to the application (site) that owns the source content:
/// <list type="number">
///   <item><description>Resolve the source content's application and its entry point (start page).</description></item>
///   <item><description>If the start page itself is of the settings type, use it (common: a StartPage that doubles as settings container).</description></item>
///   <item><description>Otherwise return the single direct child of the start page of the settings type.</description></item>
/// </list>
/// Only the start page and its direct children are inspected — this stays cheap on large sites. For a
/// settings instance placed deeper in the tree (or resolved via a settings add-on), register a custom
/// <see cref="ISettingsTargetResolver"/>.
/// </summary>
internal sealed class ContentTreeSettingsTargetResolver : ISettingsTargetResolver
{
    private readonly IContentLoader _contentLoader;
    private readonly IApplicationResolver _applicationResolver;
    private readonly ILogger<ContentTreeSettingsTargetResolver> _logger;

    public ContentTreeSettingsTargetResolver(
        IContentLoader contentLoader,
        IApplicationResolver applicationResolver,
        ILogger<ContentTreeSettingsTargetResolver> logger)
    {
        _contentLoader = contentLoader;
        _applicationResolver = applicationResolver;
        _logger = logger;
    }

    public IEnumerable<IContent> Resolve(Type settingsType, IContent source)
    {
        var startPageRef = ResolveStartPage(source);
        if (ContentReference.IsNullOrEmpty(startPageRef))
        {
            _logger.LogDebug(
                "AutoRegister: no application/start page for '{Source}' ({Link}); skipping registration into '{SettingsType}'.",
                source.Name, source.ContentLink, settingsType.Name);
            return Enumerable.Empty<IContent>();
        }

        // The start page itself may be the settings container.
        if (_contentLoader.TryGet<IContent>(startPageRef!, out var startPage) &&
            settingsType.IsInstanceOfType(startPage))
        {
            return new[] { startPage };
        }

        // Otherwise look for a single direct child of the start page of the settings type.
        var matches = _contentLoader
            .GetChildren<IContent>(startPageRef!)
            .Where(settingsType.IsInstanceOfType)
            .ToList();

        if (matches.Count == 1)
        {
            return matches;
        }

        if (matches.Count == 0)
        {
            _logger.LogDebug(
                "AutoRegister: no '{SettingsType}' found at or directly under start page {StartPage}; skipping registration for '{Source}'.",
                settingsType.Name, startPageRef, source.Name);
        }
        else
        {
            _logger.LogWarning(
                "AutoRegister: expected one '{SettingsType}' directly under start page {StartPage} but found {Count}; skipping registration for '{Source}'. Use a custom ISettingsTargetResolver to disambiguate.",
                settingsType.Name, startPageRef, matches.Count, source.Name);
        }

        return Enumerable.Empty<IContent>();
    }

    private ContentReference? ResolveStartPage(IContent source)
    {
        // Scope to the application that owns the source content (handles multi-site/multi-application).
        if (_applicationResolver.GetByContent(source.ContentLink, fallbackToDefault: true) is Website website &&
            !ContentReference.IsNullOrEmpty(website.EntryPoint))
        {
            return website.EntryPoint;
        }

        // Fallback: the start page for the current request, if any.
        return ContentReference.StartPage;
    }
}
