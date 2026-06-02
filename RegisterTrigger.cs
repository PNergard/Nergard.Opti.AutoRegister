namespace Nergard.Opti.AutoRegister;

/// <summary>
/// Determines which content event causes a <see cref="RegisterInAttribute"/> to wire the
/// created content into its settings target.
/// </summary>
public enum RegisterTrigger
{
    /// <summary>
    /// React when the content is published. This is the default — by publish time the content
    /// has a stable name and link, and editors expect references to point at published content.
    /// </summary>
    Published = 0,

    /// <summary>
    /// React as soon as the content is created (still a draft). Use when you want the settings
    /// reference wired up immediately, before the editor publishes.
    /// </summary>
    Created = 1
}
