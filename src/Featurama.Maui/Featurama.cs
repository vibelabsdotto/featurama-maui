using Featurama.Maui.Models;

namespace Featurama.Maui;

public static class Featurama
{
    private static FeaturamaClient? _client;
    private static readonly HttpClient HttpClient = new(new HttpClientHandler { AllowAutoRedirect = false })
    { Timeout = System.Threading.Timeout.InfiniteTimeSpan };

    public static bool IsInitialized => _client is not null;

    public static void Init(Action<FeaturamaOptionsBuilder> configure)
    {
        var builder = new FeaturamaOptionsBuilder();
        configure(builder);
        var options = builder.Build();

        _client = new FeaturamaClient(HttpClient, options);
    }

    public static void Init(FeaturamaOptions options)
    {
        _client = new FeaturamaClient(HttpClient, options);
    }

    internal static FeaturamaClient Client =>
        _client ?? throw new InvalidOperationException(
            "Featurama SDK has not been initialized. Call Featurama.Init() first.");

    public static Task<PaginatedResponse<FeatureRequest>> GetFeatureRequestsAsync(
        int page = 1, int pageSize = 20, string? filter = null,
        CancellationToken cancellationToken = default)
        => Client.GetFeatureRequestsAsync(page, pageSize, filter, cancellationToken);

    public static Task<FeatureRequest> CreateFeatureRequestAsync(
        string title, string? description = null, string? submitterIdentifier = null,
        CancellationToken cancellationToken = default)
        => Client.CreateFeatureRequestAsync(title, description, submitterIdentifier, cancellationToken);

    public static Task<FeatureRequest> UpdateFeatureRequestAsync(
        Guid id, string title, string? description = null, string submitterIdentifier = "",
        CancellationToken cancellationToken = default)
        => Client.UpdateFeatureRequestAsync(id, title, description, submitterIdentifier, cancellationToken);

    public static Task<FeatureRequest> VoteAsync(
        Guid featureRequestId, string voterIdentifier,
        CancellationToken cancellationToken = default)
        => Client.VoteAsync(featureRequestId, voterIdentifier, cancellationToken);

    public static Task<FeatureRequest> RemoveVoteAsync(
        Guid featureRequestId, string voterIdentifier,
        CancellationToken cancellationToken = default)
        => Client.RemoveVoteAsync(featureRequestId, voterIdentifier, cancellationToken);

    public static Task<FeatureRequest> ToggleVoteAsync(
        Guid featureRequestId, string voterIdentifier,
        CancellationToken cancellationToken = default)
        => Client.ToggleVoteAsync(featureRequestId, voterIdentifier, cancellationToken);

    public static Task<ProjectConfig> GetProjectConfigAsync(
        CancellationToken cancellationToken = default)
        => Client.GetProjectConfigAsync(cancellationToken);

    public static Task<PaginatedResponse<FeatureRequest>> GetFeatureRequestsAsyncForUser(
        string submitterIdentifier, int page = 1, int pageSize = 20, string? filter = null,
        CancellationToken cancellationToken = default)
        => Client.GetFeatureRequestsAsyncForUser(submitterIdentifier, page, pageSize, filter, cancellationToken);

    public static Task<FeatureRequest> CreateFeatureRequestAsync(CreateFeatureRequestInput input,
        CancellationToken cancellationToken = default)
        => Client.CreateFeatureRequestAsync(input, cancellationToken);

    public static Task<List<Comment>> GetCommentsAsync(Guid featureRequestId, CancellationToken cancellationToken = default)
        => Client.GetCommentsAsync(featureRequestId, cancellationToken);

    public static Task<Comment> AddCommentAsync(Guid featureRequestId, string content, string authorIdentifier,
        string? authorName = null, CancellationToken cancellationToken = default)
        => Client.AddCommentAsync(featureRequestId, content, authorIdentifier, authorName, cancellationToken);

    public static Task<Comment> VoteCommentAsync(Guid featureRequestId, Guid commentId, string voterIdentifier,
        CancellationToken cancellationToken = default)
        => Client.VoteCommentAsync(featureRequestId, commentId, voterIdentifier, cancellationToken);

    public static Task<Comment> RemoveCommentVoteAsync(Guid featureRequestId, Guid commentId, string voterIdentifier,
        CancellationToken cancellationToken = default)
        => Client.RemoveCommentVoteAsync(featureRequestId, commentId, voterIdentifier, cancellationToken);

    public static Task<Comment> ToggleCommentVoteAsync(Guid featureRequestId, Guid commentId, string voterIdentifier,
        CancellationToken cancellationToken = default)
        => Client.ToggleCommentVoteAsync(featureRequestId, commentId, voterIdentifier, cancellationToken);

    internal static void Reset() => _client = null;
}
