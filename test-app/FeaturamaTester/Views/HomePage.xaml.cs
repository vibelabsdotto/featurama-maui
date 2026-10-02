using Featurama.Maui.UI;
using Featurama.Maui.UI.Theme;
using FeaturamaTester.ViewModels;

namespace FeaturamaTester.Views;

public partial class HomePage : ContentPage
{
    private bool _navigating;

    public HomePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await Config.InitializeAsync();
            StatusLabel.Text = Config.IsConfigured
                ? $"API origin: {Config.BaseUrl}"
                : string.IsNullOrEmpty(Config.StatusMessage)
                    ? "Set an API origin and project key in Settings first."
                    : Config.StatusMessage;
        }
        catch (Exception ex) { StatusLabel.Text = $"Could not load configuration: {ex.Message}"; }
    }

    private async void OnOpenFeaturama(object sender, EventArgs e)
    {
        if (_navigating) return;
        _navigating = true;
        try
        {
            await Config.InitializeAsync();
            if (!Config.IsConfigured)
            {
                await OpenSettingsAsync();
                return;
            }
            var page = new FeaturamaPage(new FeaturamaPageOptions
            {
                AccentColor = Color.FromArgb("#6366F1"),
                ColorScheme = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? FeaturamaColorScheme.Dark : FeaturamaColorScheme.Light,
                SubmitterIdentifier = Config.UserId,
                OnCloseAsync = async () => { await Navigation.PopModalAsync(); },
            });
            await Navigation.PushModalAsync(new NavigationPage(page));
        }
        catch (Exception ex) { StatusLabel.Text = $"Could not open feature requests: {ex.Message}"; }
        finally { _navigating = false; }
    }

    private async void OnOpenSettings(object sender, EventArgs e)
    {
        if (_navigating) return;
        _navigating = true;
        try { await OpenSettingsAsync(); }
        catch (Exception ex) { StatusLabel.Text = $"Could not open settings: {ex.Message}"; }
        finally { _navigating = false; }
    }

    private Task OpenSettingsAsync() => Navigation.PushAsync(new SettingsPage(new SettingsViewModel()));
}
