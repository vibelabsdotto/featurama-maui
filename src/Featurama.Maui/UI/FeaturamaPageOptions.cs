using Featurama.Maui.UI.Strings;
using Featurama.Maui.UI.Theme;

namespace Featurama.Maui.UI;

public sealed class FeaturamaPageOptions
{
    public FeaturamaColorScheme ColorScheme { get; set; } = FeaturamaColorScheme.Light;
    public Color? AccentColor { get; set; }
    public Action? OnClose { get; set; }
    public Func<Task>? OnCloseAsync { get; set; }

    /// <summary>Stable identity used for pending requests, edits, votes and comments. Captured per page; defaults to an installation ID.</summary>
    public string? SubmitterIdentifier { get; set; }

    /// <summary>
    /// Device metadata for UI submissions. When supplied, this replaces automatic metadata
    /// entirely, including intentionally empty fields. An empty instance disables collection.
    /// Otherwise only permission-free device, OS and app version information is collected.
    /// </summary>
    public Models.DeviceInfo? DeviceInfo { get; set; }

    public FeaturamaStrings? Strings { get; set; }
}
