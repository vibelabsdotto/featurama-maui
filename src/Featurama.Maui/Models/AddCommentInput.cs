namespace Featurama.Maui.Models;

internal sealed class AddCommentInput
{
    public required string Content { get; set; }
    public required string AuthorIdentifier { get; set; }
    public string? AuthorName { get; set; }
}
