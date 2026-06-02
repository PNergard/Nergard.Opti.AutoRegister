using EPiServer.Core;
using EPiServer.DataAnnotations;
using Nergard.Opti.AutoRegister.Internal;
using Xunit;

namespace Nergard.Opti.AutoRegister.Tests;

public class PropertyMatcherTests
{
    private readonly PropertyMatcher _matcher = new();

    // --- Test fixture content models -------------------------------------------------------------

    private class SourceBase { }
    private class SourceArticle : SourceBase { }
    private class SourceContact { }
    private class Unrelated { }

    // Mirrors a real settings page: a generic slot competing with a type-specific slot.
    private class MixedSettings
    {
        [AllowedTypes(typeof(object))]
        public virtual ContentReference AnyContent { get; set; } = ContentReference.EmptyReference;

        [AllowedTypes(typeof(SourceBase))]
        public virtual ContentReference AnySource { get; set; } = ContentReference.EmptyReference;

        [AllowedTypes(typeof(SourceArticle))]
        public virtual ContentReference SpecificArticle { get; set; } = ContentReference.EmptyReference;
    }

    private class NarrowSettings
    {
        [AllowedTypes(typeof(SourceArticle))]
        public virtual ContentReference LatestArticle { get; set; } = ContentReference.EmptyReference;

        [AllowedTypes(typeof(Unrelated))]
        public virtual ContentReference Other { get; set; } = ContentReference.EmptyReference;

        public virtual string Title { get; set; } = string.Empty;
    }

    private class BroadSettings
    {
        [AllowedTypes(typeof(object))]
        public virtual ContentReference First { get; set; } = ContentReference.EmptyReference;

        [AllowedTypes(typeof(object))]
        public virtual ContentReference Second { get; set; } = ContentReference.EmptyReference;
    }

    // --- Auto-match ------------------------------------------------------------------------------

    [Fact]
    public void FindByAllowedType_returns_single_matching_property()
    {
        var match = _matcher.FindByAllowedType(typeof(NarrowSettings), typeof(SourceArticle));

        Assert.True(match.IsMatch);
        Assert.Equal(nameof(NarrowSettings.LatestArticle), match.Property!.Name);
    }

    [Fact]
    public void FindByAllowedType_returns_none_when_no_property_accepts_the_type()
    {
        var match = _matcher.FindByAllowedType(typeof(NarrowSettings), typeof(SourceContact));

        Assert.False(match.IsMatch);
        Assert.False(match.Ambiguous);
        Assert.NotNull(match.Message);
    }

    [Fact]
    public void FindByAllowedType_is_ambiguous_when_several_equally_specific_properties_match()
    {
        var match = _matcher.FindByAllowedType(typeof(BroadSettings), typeof(SourceArticle));

        Assert.False(match.IsMatch);
        Assert.True(match.Ambiguous);
        Assert.Equal(2, match.Candidates.Count);
    }

    [Fact]
    public void FindByAllowedType_prefers_the_most_specific_allowed_type_over_generic_slots()
    {
        // AnyContent (object) and AnySource (SourceBase) also accept a SourceArticle, but the
        // property declared for the concrete type must win — generic slots must not cause ambiguity.
        var match = _matcher.FindByAllowedType(typeof(MixedSettings), typeof(SourceArticle));

        Assert.True(match.IsMatch);
        Assert.Equal(nameof(MixedSettings.SpecificArticle), match.Property!.Name);
    }

    [Fact]
    public void FindByAllowedType_uses_the_closest_base_when_no_exact_type_match_exists()
    {
        // For SourceBase: AnySource (SourceBase, distance 0) beats AnyContent (object, distance 1);
        // SpecificArticle does not accept SourceBase at all.
        var match = _matcher.FindByAllowedType(typeof(MixedSettings), typeof(SourceBase));

        Assert.True(match.IsMatch);
        Assert.Equal(nameof(MixedSettings.AnySource), match.Property!.Name);
    }

    // --- Explicit by name ------------------------------------------------------------------------

    [Fact]
    public void FindByName_returns_named_content_reference_property()
    {
        var match = _matcher.FindByName(typeof(NarrowSettings), nameof(NarrowSettings.LatestArticle));

        Assert.True(match.IsMatch);
        Assert.Equal(nameof(NarrowSettings.LatestArticle), match.Property!.Name);
    }

    [Fact]
    public void FindByName_returns_none_for_missing_property()
    {
        var match = _matcher.FindByName(typeof(NarrowSettings), "DoesNotExist");

        Assert.False(match.IsMatch);
        Assert.NotNull(match.Message);
    }

    [Fact]
    public void FindByName_returns_none_for_non_content_reference_property()
    {
        var match = _matcher.FindByName(typeof(NarrowSettings), nameof(NarrowSettings.Title));

        Assert.False(match.IsMatch);
        Assert.NotNull(match.Message);
    }

    // --- Resolve dispatch ------------------------------------------------------------------------

    [Fact]
    public void Resolve_uses_explicit_name_when_provided()
    {
        var match = _matcher.Resolve(typeof(NarrowSettings), typeof(SourceContact), nameof(NarrowSettings.Other));

        Assert.True(match.IsMatch);
        Assert.Equal(nameof(NarrowSettings.Other), match.Property!.Name);
    }

    [Fact]
    public void Resolve_falls_back_to_auto_match_when_no_name()
    {
        var match = _matcher.Resolve(typeof(NarrowSettings), typeof(SourceArticle), null);

        Assert.True(match.IsMatch);
        Assert.Equal(nameof(NarrowSettings.LatestArticle), match.Property!.Name);
    }
}
