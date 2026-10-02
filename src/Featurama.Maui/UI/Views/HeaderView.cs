using Featurama.Maui.UI.Strings;
using Featurama.Maui.UI.Theme;

namespace Featurama.Maui.UI.Views;

internal sealed class HeaderView : ContentView
{
    public HeaderView(FeaturamaTheme theme, FeaturamaStrings strings, Func<Task>? onClose, Func<Task> onAdd, Action<Exception> onError)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(40),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(40),
            },
            Padding = new Thickness(8, 8, 8, 16),
        };

        if (onClose != null)
        {
            var closeBtn = new Button
            {
                Text = "\u2715",
                TextColor = theme.Text,
                BackgroundColor = Colors.Transparent,
                FontSize = 18,
                WidthRequest = 40,
                HeightRequest = 40,
            };
            SemanticProperties.SetDescription(closeBtn, strings.Close);
            closeBtn.Clicked += async (_, _) =>
            {
                closeBtn.IsEnabled = false;
                try { await onClose(); }
                catch (Exception ex) { onError(ex); }
                finally { closeBtn.IsEnabled = true; }
            };
            grid.Add(closeBtn, 0);
        }

        var title = new Label
        {
            Text = strings.Title,
            TextColor = theme.Text,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        grid.Add(title, 1);

        var addBtn = new Button
        {
            Text = "+",
            TextColor = theme.Accent,
            BackgroundColor = Colors.Transparent,
            FontSize = 22,
            WidthRequest = 40,
            HeightRequest = 40,
        };
        SemanticProperties.SetDescription(addBtn, strings.Submit);
        addBtn.Clicked += async (_, _) =>
        {
            addBtn.IsEnabled = false;
            try { await onAdd(); }
            catch (Exception ex) { onError(ex); }
            finally { addBtn.IsEnabled = true; }
        };
        grid.Add(addBtn, 2);

        Content = grid;
    }
}
