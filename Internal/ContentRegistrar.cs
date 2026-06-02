using System;
using System.Linq;
using System.Reflection;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Security;
using Microsoft.Extensions.Logging;
using Nergard.Opti.AutoRegister.Resolution;

namespace Nergard.Opti.AutoRegister.Internal;

/// <summary>
/// Default <see cref="IContentRegistrar"/>. See <see cref="RegisterInAttribute"/> for the behaviour.
/// </summary>
internal sealed class ContentRegistrar : IContentRegistrar
{
    private readonly IContentRepository _contentRepository;
    private readonly ISettingsTargetResolver _targetResolver;
    private readonly PropertyMatcher _propertyMatcher;
    private readonly AutoRegisterOptions _options;
    private readonly ILogger<ContentRegistrar> _logger;

    public ContentRegistrar(
        IContentRepository contentRepository,
        ISettingsTargetResolver targetResolver,
        PropertyMatcher propertyMatcher,
        AutoRegisterOptions options,
        ILogger<ContentRegistrar> logger)
    {
        _contentRepository = contentRepository;
        _targetResolver = targetResolver;
        _propertyMatcher = propertyMatcher;
        _options = options;
        _logger = logger;
    }

    public void Handle(IContent source, RegisterTrigger firedTrigger)
    {
        // v1: pages only.
        if (source is not PageData)
        {
            return;
        }

        var sourceType = source.GetOriginalType();
        var attributes = sourceType
            .GetCustomAttributes<RegisterInAttribute>(inherit: true)
            .Where(a => a.Trigger == firedTrigger)
            .ToList();

        if (attributes.Count == 0)
        {
            return;
        }

        foreach (var attribute in attributes)
        {
            ApplyAttribute(source, sourceType, attribute);
        }
    }

    private void ApplyAttribute(IContent source, Type sourceType, RegisterInAttribute attribute)
    {
        foreach (var target in _targetResolver.Resolve(attribute.SettingsType, source))
        {
            // Never register a piece of content into itself.
            if (target.ContentLink.CompareToIgnoreWorkID(source.ContentLink))
            {
                continue;
            }

            var match = _propertyMatcher.Resolve(attribute.SettingsType, sourceType, attribute.PropertyName);
            if (!match.IsMatch)
            {
                _logger.LogWarning("AutoRegister: {Message}", match.Message);
                continue;
            }

            TrySetReference(source, target, match.Property!);
        }
    }

    private void TrySetReference(IContent source, IContent target, PropertyInfo property)
    {
        var current = property.GetValue(target) as ContentReference;
        if (!ContentReference.IsNullOrEmpty(current))
        {
            // Empty-only: never overwrite an existing reference.
            return;
        }

        if (target is not ContentData contentData || contentData.CreateWritableClone() is not IContent clone)
        {
            return;
        }

        property.SetValue(clone, source.ContentLink);

        var saveAction = _options.RepublishTargetOnChange ? SaveAction.Publish : SaveAction.Save;

        try
        {
            _contentRepository.Save(clone, saveAction, AccessLevel.NoAccess);
            _logger.LogInformation(
                "AutoRegister: wired '{Source}' ({SourceLink}) into {SettingsType}.{Property}.",
                source.Name, source.ContentLink, target.GetOriginalType().Name, property.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "AutoRegister: failed to set {SettingsType}.{Property} to '{Source}'.",
                target.GetOriginalType().Name, property.Name, source.Name);
        }
    }
}
