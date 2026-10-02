namespace Featurama.Maui.Models;

public sealed class Comment
{
    public Guid Id { get; set; }
    public Guid FeatureRequestId { get; set; }
    public string Content { get; set; } = "";
    public string AuthorIdentifier { get; set; } = "";
    public string? AuthorName { get; set; }
    public string AuthorRole { get; set; } = "user";
    public int VoteCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
