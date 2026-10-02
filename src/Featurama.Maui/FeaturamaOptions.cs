namespace Featurama.Maui;

public sealed class FeaturamaOptions
{
    public const string DefaultBaseUrl = "https://newapi.featurama.app";
    public const string LegacyBaseUrl = "https://api.featurama.app";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = DefaultBaseUrl;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey) || ApiKey.Contains('\r') || ApiKey.Contains('\n'))
            throw new ArgumentException("API key must not be empty or contain line breaks.");
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("Base URL must be an absolute HTTP(S) URL without credentials, query or fragment.");
        if (Timeout != System.Threading.Timeout.InfiniteTimeSpan &&
            (Timeout <= TimeSpan.Zero || Timeout.TotalMilliseconds > uint.MaxValue - 1))
            throw new ArgumentOutOfRangeException(nameof(Timeout), "Timeout must be positive and fit a cancellation timer, or be infinite.");
    }
}
