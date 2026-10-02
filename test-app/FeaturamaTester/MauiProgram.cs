using FeaturamaTester.ViewModels;
using FeaturamaTester.Views;

namespace FeaturamaTester;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // The native SDK page uses the static facade. Config initializes that same
        // client after secure settings load, and replaces it on every settings save.
        // Do not register a second DI client with stale startup credentials.

        // Register ViewModels
        builder.Services.AddTransient<SettingsViewModel>();

        // Register Pages
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}
