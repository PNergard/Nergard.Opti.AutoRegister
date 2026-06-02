namespace Nergard.Opti.AutoRegister;

/// <summary>
/// Options for the AutoRegister feature. Configure via
/// <see cref="ServiceCollectionExtensions.AddNergardAutoRegister"/>.
/// </summary>
public sealed class AutoRegisterOptions
{
    /// <summary>
    /// When <c>true</c> (default) the settings instance is saved and published after a reference is
    /// wired in, so the change is immediately live. Reserved for a future save-as-draft mode.
    /// </summary>
    public bool RepublishTargetOnChange { get; set; } = true;
}
