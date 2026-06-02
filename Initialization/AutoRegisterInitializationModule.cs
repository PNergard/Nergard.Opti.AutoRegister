using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.DependencyInjection;
using Nergard.Opti.AutoRegister.Internal;

namespace Nergard.Opti.AutoRegister.Initialization;

/// <summary>
/// Registers the AutoRegister services and subscribes to content events so decorated content is
/// wired into its settings target on creation/publish. No Startup configuration required.
/// </summary>
[InitializableModule]
public class AutoRegisterInitializationModule : IConfigurableModule
{
    private IContentEvents? _contentEvents;
    private IContentRegistrar? _registrar;

    public void ConfigureContainer(ServiceConfigurationContext context)
    {
        context.Services.AddNergardAutoRegister();
    }

    public void Initialize(InitializationEngine context)
    {
        _contentEvents = context.Services.GetRequiredService<IContentEvents>();
        _registrar = context.Services.GetRequiredService<IContentRegistrar>();

        _contentEvents.PublishedContent += OnPublishedContent;
        _contentEvents.CreatedContent += OnCreatedContent;
    }

    public void Uninitialize(InitializationEngine context)
    {
        if (_contentEvents is null)
        {
            return;
        }

        _contentEvents.PublishedContent -= OnPublishedContent;
        _contentEvents.CreatedContent -= OnCreatedContent;
    }

    private void OnPublishedContent(object? sender, ContentEventArgs e)
    {
        if (e.Content is IContent content)
        {
            _registrar?.Handle(content, RegisterTrigger.Published);
        }
    }

    private void OnCreatedContent(object? sender, ContentEventArgs e)
    {
        if (e.Content is IContent content)
        {
            _registrar?.Handle(content, RegisterTrigger.Created);
        }
    }
}
