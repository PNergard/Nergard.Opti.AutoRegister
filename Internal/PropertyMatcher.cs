using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace Nergard.Opti.AutoRegister.Internal;

/// <summary>
/// Outcome of resolving which property on a settings type should receive a reference.
/// </summary>
internal readonly struct PropertyMatch
{
    private PropertyMatch(PropertyInfo? property, bool ambiguous, string? message, IReadOnlyList<string> candidates)
    {
        Property = property;
        Ambiguous = ambiguous;
        Message = message;
        Candidates = candidates;
    }

    /// <summary>The matched target property, or null when no usable match was found.</summary>
    public PropertyInfo? Property { get; }

    /// <summary>True when more than one property matched and an explicit name is required.</summary>
    public bool Ambiguous { get; }

    /// <summary>Diagnostic message for the no-match / ambiguous cases.</summary>
    public string? Message { get; }

    /// <summary>Names of the candidate properties (populated for the ambiguous case).</summary>
    public IReadOnlyList<string> Candidates { get; }

    public bool IsMatch => Property is not null;

    public static PropertyMatch Match(PropertyInfo property) =>
        new(property, ambiguous: false, message: null, Array.Empty<string>());

    public static PropertyMatch None(string message) =>
        new(null, ambiguous: false, message, Array.Empty<string>());

    public static PropertyMatch AmbiguousMatch(string message, IReadOnlyList<string> candidates) =>
        new(null, ambiguous: true, message, candidates);
}

/// <summary>
/// Pure reflection helper that locates the single, empty-eligible <see cref="ContentReference"/>
/// property on a settings type that a created content should be wired into.
/// </summary>
internal sealed class PropertyMatcher
{
    /// <summary>
    /// Resolves the target property on <paramref name="settingsType"/> for the given
    /// <paramref name="sourceType"/>. When <paramref name="explicitPropertyName"/> is provided it is
    /// used directly; otherwise the property is auto-matched by its <c>[AllowedTypes]</c>.
    /// </summary>
    public PropertyMatch Resolve(Type settingsType, Type sourceType, string? explicitPropertyName)
    {
        return string.IsNullOrWhiteSpace(explicitPropertyName)
            ? FindByAllowedType(settingsType, sourceType)
            : FindByName(settingsType, explicitPropertyName!);
    }

    /// <summary>Finds a named single <see cref="ContentReference"/> property.</summary>
    public PropertyMatch FindByName(Type settingsType, string propertyName)
    {
        var property = settingsType.GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property is null)
        {
            return PropertyMatch.None(
                $"Property '{propertyName}' was not found on settings type '{settingsType.Name}'.");
        }

        if (!IsSingleContentReference(property))
        {
            return PropertyMatch.None(
                $"Property '{settingsType.Name}.{propertyName}' is not a single ContentReference and cannot be auto-registered.");
        }

        return PropertyMatch.Match(property);
    }

    /// <summary>
    /// Finds the <see cref="ContentReference"/> property whose <c>[AllowedTypes]</c> most specifically
    /// permits <paramref name="sourceType"/>. A property targeting the concrete type (or a closer base)
    /// wins over generic slots such as <c>[AllowedTypes(typeof(PageData))]</c>. Only a tie at the same
    /// specificity is reported as ambiguous.
    /// </summary>
    public PropertyMatch FindByAllowedType(Type settingsType, Type sourceType)
    {
        var candidates = settingsType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(IsSingleContentReference)
            .Select(p => (Property: p, Distance: Specificity(p, sourceType)))
            .Where(x => x.Distance >= 0)
            .ToList();

        if (candidates.Count == 0)
        {
            return PropertyMatch.None(
                $"No ContentReference property on '{settingsType.Name}' has [AllowedTypes] permitting '{sourceType.Name}'. " +
                "Add [AllowedTypes(typeof(...))] to the target property or specify a property name on [RegisterIn].");
        }

        var bestDistance = candidates.Min(x => x.Distance);
        var best = candidates.Where(x => x.Distance == bestDistance).ToList();

        if (best.Count > 1)
        {
            var names = best.Select(x => x.Property.Name).ToList();
            return PropertyMatch.AmbiguousMatch(
                $"Multiple properties on '{settingsType.Name}' accept '{sourceType.Name}' equally specifically ({string.Join(", ", names)}). " +
                "Specify a property name on [RegisterIn] to disambiguate.",
                names);
        }

        return PropertyMatch.Match(best[0].Property);
    }

    private static bool IsSingleContentReference(PropertyInfo property) =>
        property.PropertyType == typeof(ContentReference);

    /// <summary>
    /// Returns how specifically a property's <c>[AllowedTypes]</c> accepts <paramref name="sourceType"/>:
    /// 0 for an exact type match, a small number for a closer base class, larger for a more distant base
    /// or an interface, and -1 when the property does not accept the type at all.
    /// </summary>
    private static int Specificity(PropertyInfo property, Type sourceType)
    {
        var allowed = property.GetCustomAttribute<AllowedTypesAttribute>();
        if (allowed?.AllowedTypes is null || allowed.AllowedTypes.Length == 0)
        {
            return -1;
        }

        var best = -1;
        foreach (var allowedType in allowed.AllowedTypes.Where(t => t.IsAssignableFrom(sourceType)))
        {
            var distance = Distance(sourceType, allowedType);
            if (best < 0 || distance < best)
            {
                best = distance;
            }
        }

        return best;
    }

    /// <summary>Inheritance distance from <paramref name="sourceType"/> up to <paramref name="target"/>.</summary>
    private const int InterfaceDistance = 10_000;

    private static int Distance(Type sourceType, Type target)
    {
        var depth = 0;
        for (var current = sourceType; current is not null; current = current.BaseType, depth++)
        {
            if (current == target)
            {
                return depth;
            }
        }

        // Assignable but not on the class chain (e.g. an interface): least specific.
        return InterfaceDistance;
    }
}
