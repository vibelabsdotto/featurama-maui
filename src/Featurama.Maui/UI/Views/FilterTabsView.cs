using Featurama.Maui.UI.Strings;
using Featurama.Maui.UI.Theme;

namespace Featurama.Maui.UI.Views;

internal sealed class FilterTabsView : ContentView
{
    private readonly FeaturamaTheme _theme;
    private readonly List<(string key, string label, Button button)> _filters;
    private string _activeFilter = "new";

    public event EventHandler<string>? FilterChanged;

    public FilterTabsView(FeaturamaTheme theme, FeaturamaStrings strings)
    {
        _theme = theme;
        _filters = new List<(string, string, Button)>
        {
            ("new", strings.FilterNew, null!),
            ("planned", strings.FilterPlanned, null!),
            ("in_progress", strings.FilterInProgress, null!),
            ("done", strings.FilterDone, null!),
        };

        var grid = new Grid
        {
            ColumnSpacing = 0,
            Padding = new Thickness(4),
            BackgroundColor = theme.Gray100,
        };

        for (var i = 0; i < _filters.Count; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        for (var i = 0; i < _filters.Count; i++)
        {
            var (key, label, _) = _filters[i];
            var btn = CreateButton(key, label);
            _filters[i] = (key, label, btn);
            grid.Add(btn, i);
        }

        var frame = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            BackgroundColor = theme.Gray100,
            StrokeThickness = 0,
            Padding = new Thickness(4),
            Content = grid,
        };

        Content = new StackLayout
        {
            Padding = new Thickness(16, 0),
            Children = { frame },
        };

        UpdateVisuals();
    }

    private Button CreateButton(string key, string label)
    {
        var btn = new Button
        {
            Text = label,
            FontSize = 14,
            HeightRequest = 36,
            Padding = new Thickness(0),
            BorderWidth = 0,
            CornerRadius = 8,
        };
        btn.Clicked += (_, _) =>
        {
            _activeFilter = key;
            UpdateVisuals();
            FilterChanged?.Invoke(this, key);
        };
        return btn;
    }

    public void SetFilter(string filter)
    {
        _activeFilter = filter;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        foreach (var (key, _, btn) in _filters)
        {
            var isActive = key == _activeFilter;
            btn.BackgroundColor = isActive ? _theme.Card : Colors.Transparent;
            btn.TextColor = isActive ? _theme.Text : _theme.TextSecondary;
            btn.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
        }
    }
}
