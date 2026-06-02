using System;

namespace Nergard.Opti.AutoRegister;

/// <summary>
/// Decorate a content type with this attribute to have a created/published instance of it
/// automatically registered into a property on a settings instance.
/// </summary>
/// <remarks>
/// <para>
/// The attribute names the settings <em>type</em> (e.g. a <c>StartPage</c> or a dedicated settings
/// page). At runtime the matching settings <em>instance</em> is resolved (site-scoped) and the new
/// content is written into a single, currently-empty <c>ContentReference</c> property on it.
/// </para>
/// <para>The target property is chosen in one of two ways:</para>
/// <list type="bullet">
///   <item>
///     <description>
///     If <see cref="PropertyName"/> is set, that property is used (explicit, recommended when the
///     settings property allows a broad base type such as <c>PageData</c>).
///     </description>
///   </item>
///   <item>
///     <description>
///     Otherwise the property whose <c>[AllowedTypes]</c> most specifically accepts the created type
///     is used — a type-specific slot wins over a generic one such as <c>[AllowedTypes(typeof(PageData))]</c>.
///     Only an equally-specific tie is skipped and logged; add a <see cref="PropertyName"/> to disambiguate.
///     </description>
///   </item>
/// </list>
/// <para>
/// The attribute can be applied multiple times to register a type into several settings targets.
/// Existing (non-empty) references are never overwritten.
/// </para>
/// <example>
/// <code>
/// [RegisterIn(typeof(StartPage))]                                       // auto-match by AllowedTypes
/// [RegisterIn(typeof(StartPage), nameof(StartPage.ContactsPageLink))]   // explicit property
/// [RegisterIn(typeof(StartPage), Trigger = RegisterTrigger.Created)]    // wire on create
/// public class ContactPage : SitePageData { }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RegisterInAttribute : Attribute
{
    /// <summary>
    /// Registers the decorated type into <paramref name="settingsType"/>, auto-matching the target
    /// property by its <c>[AllowedTypes]</c>.
    /// </summary>
    /// <param name="settingsType">The settings content type that holds the target property.</param>
    public RegisterInAttribute(Type settingsType)
    {
        SettingsType = settingsType ?? throw new ArgumentNullException(nameof(settingsType));
    }

    /// <summary>
    /// Registers the decorated type into a specific property on <paramref name="settingsType"/>.
    /// </summary>
    /// <param name="settingsType">The settings content type that holds the target property.</param>
    /// <param name="propertyName">The name of the target <c>ContentReference</c> property.</param>
    public RegisterInAttribute(Type settingsType, string propertyName)
        : this(settingsType)
    {
        PropertyName = propertyName;
    }

    /// <summary>The settings content type whose instance receives the reference.</summary>
    public Type SettingsType { get; }

    /// <summary>
    /// Optional explicit target property name. When null, the target is auto-matched via
    /// <c>[AllowedTypes]</c>.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>The event that triggers registration. Defaults to <see cref="RegisterTrigger.Published"/>.</summary>
    public RegisterTrigger Trigger { get; init; } = RegisterTrigger.Published;
}
