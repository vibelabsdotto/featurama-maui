namespace Featurama.Maui.Models;

public sealed class FeatureRequest
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FeatureRequestStatus Status { get; set; }
    public FeatureRequestSource Source { get; set; }
    public int VoteCount { get; set; }
    public string? SubmitterIdentifier { get; set; }
    public DateTime CreatedAt { get; set; }
    // Legacy API responses omit approval and represent visible requests.
    public bool IsApproved { get; set; } = true;
    public bool HasVoted { get; set; }
    public int CommentCount { get; set; }
    public string? SubmitterEmail { get; set; }
    public DeviceInfo? DeviceInfo { get; set; }
}
