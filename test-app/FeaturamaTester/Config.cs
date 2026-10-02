using System.Text.Json;
using System.Text.RegularExpressions;
using Featurama.Maui;

namespace FeaturamaTester;

public static class Config
{
    private const string SettingsKey = "featurama_tester_connection";
    private const string IdentityKey = "featurama_voter_id";
    private static Task? _initializeTask;

    public static string ApiKey { get; private set; } = "";
    public static string BaseUrl { get; private set; } = "";
    public static string UserId { get; private set; } = "";
    public static string StatusMessage { get; private set; } = "";
    public static bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(BaseUrl);

    public static Task InitializeAsync() => _initializeTask ??= LoadAsync();

    private static async Task LoadAsync()
    {
        UserId = Preferences.Get(IdentityKey, "");
        if (string.IsNullOrWhiteSpace(UserId))
        {
            UserId = Guid.NewGuid().ToString();
            Preferences.Set(IdentityKey, UserId);
        }
        try
        {
            var json = await SecureStorage.Default.GetAsync(SettingsKey);
            if (string.IsNullOrEmpty(json)) return;
            var settings = JsonSerializer.Deserialize<SavedConnection>(json);
            if (settings == null) return;
            var origin = ValidateOrigin(settings.BaseUrl);
            if (string.IsNullOrWhiteSpace(settings.ApiKey)) return;
            Apply(settings.ApiKey, origin);
        }
        catch
        {
            // Secure storage may be unavailable or restored from another device.
            // Never silently substitute a different API origin for a saved key.
            StatusMessage = "Saved connection could not be restored. Enter the API key and origin in Settings.";
        }
    }

    public static async Task ApplyAsync(string apiKey, string baseUrl)
    {
        await InitializeAsync();
        var key = apiKey.Trim();
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("API key cannot be empty.");
        var origin = ValidateOrigin(baseUrl);
        Apply(key, origin);
        try
        {
            // Store the origin and key together to avoid restoring mismatched credentials.
            await SecureStorage.Default.SetAsync(SettingsKey,
                JsonSerializer.Serialize(new SavedConnection { ApiKey = key, BaseUrl = origin }));
            StatusMessage = "Settings saved and applied. Open Feature Requests to use this connection.";
        }
        catch
        {
            StatusMessage = "Settings applied for this session, but secure storage is unavailable. Re-enter the connection after restarting.";
        }
    }

    private static void Apply(string apiKey, string origin)
    {
        global::Featurama.Maui.Featurama.Init(new FeaturamaOptions { ApiKey = apiKey, BaseUrl = origin });
        ApiKey = apiKey;
        BaseUrl = origin;
    }

    public static string ValidateOrigin(string value)
    {
        var text = value.Trim();
        if (!Regex.IsMatch(text, @"^https?://[^/\\?#\s]+/?$", RegexOptions.IgnoreCase) ||
            !Uri.TryCreate(text, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("Enter an explicit HTTP(S) API origin, including its port if needed, without /api/public, credentials, a query or fragment.");
        return text.TrimEnd('/');
    }

    private sealed class SavedConnection
    {
        public SavedConnection() { }
        public string ApiKey { get; set; } = "";
        public string BaseUrl { get; set; } = "";
    }
}
