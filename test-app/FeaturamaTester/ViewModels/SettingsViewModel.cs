using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FeaturamaTester.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _apiKey = "";

    [ObservableProperty]
    private string _baseUrl = "";

    [ObservableProperty]
    private string _userId = "";

    [ObservableProperty]
    private string _statusMessage = "";

    public async Task LoadAsync()
    {
        try
        {
            await Config.InitializeAsync();
            ApiKey = Config.ApiKey;
            BaseUrl = Config.BaseUrl;
            UserId = Config.UserId;
            StatusMessage = Config.StatusMessage;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not load settings: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await Config.ApplyAsync(ApiKey, BaseUrl);
            ApiKey = Config.ApiKey;
            BaseUrl = Config.BaseUrl;
            UserId = Config.UserId;
            StatusMessage = Config.StatusMessage;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Settings were not applied: {ex.Message}";
        }
    }
}
