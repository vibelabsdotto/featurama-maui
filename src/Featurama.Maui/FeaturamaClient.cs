using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Web;
using Featurama.Maui.Exceptions;
using Featurama.Maui.Models;
using Featurama.Maui.Serialization;

namespace Featurama.Maui;

public sealed class FeaturamaClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly TimeSpan _timeout;

    public FeaturamaClient(HttpClient httpClient, FeaturamaOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _httpClient = httpClient;
        _baseUrl = options.BaseUrl.TrimEnd('/');
        _apiKey = options.ApiKey;
        _timeout = options.Timeout;
    }

    public async Task<PaginatedResponse<FeatureRequest>> GetFeatureRequestsAsync(
        int page = 1,
        int pageSize = 20,
        string? filter = null,
        CancellationToken cancellationToken = default)
        => await GetFeatureRequestsCoreAsync(page, pageSize, filter, null, cancellationToken);

    public Task<PaginatedResponse<FeatureRequest>> GetFeatureRequestsAsyncForUser(
        string submitterIdentifier, int page = 1, int pageSize = 20, string? filter = null,
        CancellationToken cancellationToken = default)
        => GetFeatureRequestsCoreAsync(page, pageSize, filter, submitterIdentifier, cancellationToken);

    private async Task<PaginatedResponse<FeatureRequest>> GetFeatureRequestsCoreAsync(
        int page, int pageSize, string? filter, string? submitterIdentifier, CancellationToken cancellationToken)
    {
        var url = $"{_baseUrl}/api/public/requests?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(submitterIdentifier))
            url += $"&submitterIdentifier={HttpUtility.UrlEncode(submitterIdentifier)}";
        if (!string.IsNullOrEmpty(filter))
            url += $"&filter={HttpUtility.UrlEncode(filter)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync(request, FeaturamaJsonContext.Default.PaginatedResponseFeatureRequest, cancellationToken);
    }

    public async Task<FeatureRequest> CreateFeatureRequestAsync(
        string title,
        string? description = null,
        string? submitterIdentifier = null,
        CancellationToken cancellationToken = default)
    {
        var body = new CreateFeatureRequestInput
        {
            Title = title,
            Description = description,
            SubmitterIdentifier = submitterIdentifier
        };

        return await CreateFeatureRequestAsync(body, cancellationToken);
    }

    public async Task<FeatureRequest> CreateFeatureRequestAsync(
        CreateFeatureRequestInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
#if ANDROID || IOS || MACCATALYST
        // Native callers receive the same permission-free metadata as the built-in form.
        // Do not mutate input; a caller can reuse it across clients or async requests.
        input = new CreateFeatureRequestInput
        {
            Title = input.Title, Description = input.Description,
            SubmitterIdentifier = input.SubmitterIdentifier, Email = input.Email,
            DeviceInfo = UI.Utils.DeviceMetadataProvider.GetDeviceInfo(input.DeviceInfo)
        };
#endif
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/public/requests")
        {
            Content = JsonContent.Create(input, FeaturamaJsonContext.Default.CreateFeatureRequestInput)
        };
        return await SendAsync(request, FeaturamaJsonContext.Default.FeatureRequest, cancellationToken);
    }

    public async Task<FeatureRequest> UpdateFeatureRequestAsync(
        Guid id,
        string title,
        string? description = null,
        string submitterIdentifier = "",
        CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/public/requests/{id}?submitterIdentifier={HttpUtility.UrlEncode(submitterIdentifier)}";
        var body = new UpdateFeatureRequestInput
        {
            Title = title,
            Description = description
        };

        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(body, FeaturamaJsonContext.Default.UpdateFeatureRequestInput)
        };
        return await SendAsync(request, FeaturamaJsonContext.Default.FeatureRequest, cancellationToken);
    }

    public async Task<FeatureRequest> VoteAsync(
        Guid featureRequestId,
        string voterIdentifier,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/public/requests/{featureRequestId}/vote";
        var body = new VoteRequestBody { VoterIdentifier = voterIdentifier };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, FeaturamaJsonContext.Default.VoteRequestBody)
        };
        return await SendAsync(request, FeaturamaJsonContext.Default.FeatureRequest, cancellationToken);
    }

    public async Task<FeatureRequest> RemoveVoteAsync(
        Guid featureRequestId,
        string voterIdentifier,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/public/requests/{featureRequestId}/vote";
        var body = new VoteRequestBody { VoterIdentifier = voterIdentifier };

        using var request = new HttpRequestMessage(HttpMethod.Delete, url)
        {
            Content = JsonContent.Create(body, FeaturamaJsonContext.Default.VoteRequestBody)
        };
        return await SendAsync(request, FeaturamaJsonContext.Default.FeatureRequest, cancellationToken);
    }

    public async Task<FeatureRequest> ToggleVoteAsync(
        Guid featureRequestId,
        string voterIdentifier,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await VoteAsync(featureRequestId, voterIdentifier, cancellationToken);
        }
        catch (FeaturamaConflictException)
        {
            return await RemoveVoteAsync(featureRequestId, voterIdentifier, cancellationToken);
        }
    }

    public async Task<ProjectConfig> GetProjectConfigAsync(
        CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/public/config";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync(request, FeaturamaJsonContext.Default.ProjectConfig, cancellationToken);
    }

    public async Task<List<Comment>> GetCommentsAsync(Guid featureRequestId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/public/requests/{featureRequestId}/comments");
        return await SendAsync(request, FeaturamaJsonContext.Default.ListComment, cancellationToken);
    }

    public async Task<Comment> AddCommentAsync(Guid featureRequestId, string content, string authorIdentifier,
        string? authorName = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/public/requests/{featureRequestId}/comments")
        {
            Content = JsonContent.Create(new AddCommentInput
            {
                Content = content, AuthorIdentifier = authorIdentifier, AuthorName = authorName
            }, FeaturamaJsonContext.Default.AddCommentInput)
        };
        return await SendAsync(request, FeaturamaJsonContext.Default.Comment, cancellationToken);
    }

    public Task<Comment> VoteCommentAsync(Guid featureRequestId, Guid commentId, string voterIdentifier,
        CancellationToken cancellationToken = default)
        => SendCommentVoteAsync(HttpMethod.Post, featureRequestId, commentId, voterIdentifier, cancellationToken);

    public Task<Comment> RemoveCommentVoteAsync(Guid featureRequestId, Guid commentId, string voterIdentifier,
        CancellationToken cancellationToken = default)
        => SendCommentVoteAsync(HttpMethod.Delete, featureRequestId, commentId, voterIdentifier, cancellationToken);

    public async Task<Comment> ToggleCommentVoteAsync(Guid featureRequestId, Guid commentId, string voterIdentifier,
        CancellationToken cancellationToken = default)
    {
        try { return await VoteCommentAsync(featureRequestId, commentId, voterIdentifier, cancellationToken); }
        catch (FeaturamaConflictException)
        { return await RemoveCommentVoteAsync(featureRequestId, commentId, voterIdentifier, cancellationToken); }
    }

    private async Task<Comment> SendCommentVoteAsync(HttpMethod method, Guid featureRequestId, Guid commentId,
        string voterIdentifier, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, $"{_baseUrl}/api/public/requests/{featureRequestId}/comments/{commentId}/vote")
        {
            Content = JsonContent.Create(new VoteRequestBody { VoterIdentifier = voterIdentifier }, FeaturamaJsonContext.Default.VoteRequestBody)
        };
        return await SendAsync(request, FeaturamaJsonContext.Default.Comment, cancellationToken);
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
    {
        // Credentials belong to this request, not to a potentially shared HttpClient.
        request.Headers.Add("X-Api-Key", _apiKey);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        try
        {
            using var response = await _httpClient.SendAsync(request, timeout.Token);
            var responseBody = await response.Content.ReadAsStringAsync(timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw (int)response.StatusCode switch
                {
                    401 => new FeaturamaUnauthorizedException(responseBody),
                    403 => new FeaturamaForbiddenException(responseBody),
                    404 => new FeaturamaNotFoundException(responseBody),
                    409 => new FeaturamaConflictException(responseBody),
                    _ => new FeaturamaApiException((int)response.StatusCode,
                        $"API error: {(int)response.StatusCode}", responseBody)
                };
            }
            try
            {
                return JsonSerializer.Deserialize(responseBody, typeInfo)
                    ?? throw new FeaturamaException("Failed to deserialize response.");
            }
            catch (JsonException ex)
            {
                throw new FeaturamaException("Failed to deserialize response.", ex);
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new FeaturamaNetworkException("Request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new FeaturamaNetworkException("Network request failed.", ex);
        }
    }
}
