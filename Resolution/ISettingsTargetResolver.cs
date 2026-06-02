using System;
using System.Collections.Generic;
using EPiServer.Core;

namespace Nergard.Opti.AutoRegister.Resolution;

/// <summary>
/// Resolves the settings content instance(s) of a given type that should receive a reference to a
/// newly created/published piece of content.
/// </summary>
/// <remarks>
/// The default implementation, <see cref="ContentTreeSettingsTargetResolver"/>, looks up a single
/// instance in the content tree scoped to the source content's site. Replace it (register your own
/// implementation after <c>AddNergardAutoRegister</c>) to resolve global settings instead — for
/// example through AddOn.Episerver.Settings' <c>ISettingsService</c>.
/// </remarks>
public interface ISettingsTargetResolver
{
    /// <summary>
    /// Returns the settings instance(s) of <paramref name="settingsType"/> to update for the given
    /// <paramref name="source"/> content. Return an empty sequence when no unambiguous target exists.
    /// </summary>
    /// <param name="settingsType">The settings content type named by the attribute.</param>
    /// <param name="source">The content that was created or published.</param>
    IEnumerable<IContent> Resolve(Type settingsType, IContent source);
}
