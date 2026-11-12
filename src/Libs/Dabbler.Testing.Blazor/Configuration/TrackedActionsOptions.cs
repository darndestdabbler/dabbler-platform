namespace Dabbler.Testing.Blazor.Configuration;

/// <summary>
/// Configuration options for action tracking in Blazor Server components.
/// </summary>
public class TrackedActionsOptions
{
    /// <summary>
    /// Whether to enable action watermark tracking on components that inherit from TrackedComponentBase.
    /// When disabled, TrackAction() is a transparent pass-through with no JS interop overhead.
    /// </summary>
    public bool Enabled { get; set; } = false;
}
