using EPiServer.Core;

namespace Nergard.Opti.AutoRegister.Internal;

/// <summary>
/// Handles a content event by wiring the content into the settings properties declared via
/// <see cref="RegisterInAttribute"/>.
/// </summary>
internal interface IContentRegistrar
{
    /// <summary>
    /// Processes <paramref name="source"/> for the event that just fired
    /// (<paramref name="firedTrigger"/>), applying every matching <see cref="RegisterInAttribute"/>.
    /// </summary>
    void Handle(IContent source, RegisterTrigger firedTrigger);
}
