namespace Featurama.Maui.Models;

public sealed class CreateFeatureRequestInput
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? SubmitterIdentifier { get; set; }
    public string? Email { get; set; }
    public DeviceInfo? DeviceInfo { get; set; }
}
