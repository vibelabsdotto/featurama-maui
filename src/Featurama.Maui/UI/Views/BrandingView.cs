using Featurama.Maui.UI.Theme;

namespace Featurama.Maui.UI.Views;

internal sealed class BrandingView : ContentView
{
    public BrandingView(FeaturamaTheme theme)
    {
        var logoIcon = new Border
        {
            WidthRequest = 16,
            HeightRequest = 16,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 },
            StrokeThickness = 0,
            Padding = 0,
            BackgroundColor = theme.Accent,
            Content = new Label
            {
                Text = "F",
                TextColor = Colors.White,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            },
        };

        var label = new Label
        {
            Text = "Powered by Featurama",
            TextColor = theme.TextSecondary,
            FontSize = 11,
            FontAttributes = FontAttributes.None,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var stack = new HorizontalStackLayout
        {
            Spacing = 6,
            VerticalOptions = LayoutOptions.Center,
            Children = { logoIcon, label },
        };

        var border = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            Stroke = Colors.Transparent,
            BackgroundColor = theme.Gray100,
            Padding = new Thickness(14, 8),
            HorizontalOptions = LayoutOptions.Center,
            Content = stack,
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (_, _) =>
        {
            try
            {
                await Launcher.OpenAsync(new Uri("https://featurama.app"));
            }
            catch
            {
                // Ignore if launcher is unavailable
            }
        };
        border.GestureRecognizers.Add(tapGesture);

        HorizontalOptions = LayoutOptions.Center;
        Content = border;
    }
}
