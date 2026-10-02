using Featurama.Maui.Models;
using Featurama.Maui.UI.Strings;
using Featurama.Maui.UI.Theme;

namespace Featurama.Maui.UI.Views;

internal sealed class EditRequestView : ContentView
{
    private readonly Entry _titleEntry;
    private readonly Editor _descriptionEditor;
    private readonly Label _errorLabel;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;
    private readonly FeaturamaStrings _strings;
    private readonly Func<string, string, Task> _onSave;
    private bool _isSaving;

    public EditRequestView(FeaturamaTheme theme, FeaturamaStrings strings, FeatureRequest request,
        Func<string, string, Task> onSave, Action onCancel)
    {
        _strings = strings;
        _onSave = onSave;
        _titleEntry = new Entry
        {
            Text = request.Title,
            Placeholder = strings.TitlePlaceholder,
            PlaceholderColor = theme.TextSecondary,
            TextColor = theme.Text,
            BackgroundColor = theme.Secondary,
            FontSize = 16,
        };
        _descriptionEditor = new Editor
        {
            Text = request.Description,
            Placeholder = strings.DescriptionPlaceholder,
            PlaceholderColor = theme.TextSecondary,
            TextColor = theme.Text,
            BackgroundColor = theme.Secondary,
            FontSize = 16,
            HeightRequest = 120,
        };
        SemanticProperties.SetDescription(_titleEntry, strings.TitlePlaceholder);
        SemanticProperties.SetDescription(_descriptionEditor, strings.DescriptionPlaceholder);
        _errorLabel = new Label { TextColor = theme.Text, IsVisible = false };
        _cancelButton = new Button
        {
            Text = strings.Cancel,
            TextColor = theme.Text,
            BackgroundColor = theme.Secondary,
            CornerRadius = 8,
        };
        _cancelButton.Clicked += (_, _) => { if (!_isSaving) onCancel(); };
        _saveButton = new Button
        {
            Text = strings.Save,
            TextColor = theme.AccentForeground,
            BackgroundColor = theme.Accent,
            CornerRadius = 8,
            FontAttributes = FontAttributes.Bold,
        };
        _saveButton.Clicked += async (_, _) => await SaveAsync();
        var buttons = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 12,
        };
        buttons.Add(_cancelButton, 0);
        buttons.Add(_saveButton, 1);
        Content = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = new SolidColorBrush(theme.BorderAccent),
            BackgroundColor = theme.Card,
            StrokeThickness = 1,
            Padding = 16,
            Margin = new Thickness(0, 0, 0, 16),
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = strings.EditRequest, TextColor = theme.Text, FontSize = 18, FontAttributes = FontAttributes.Bold },
                    _titleEntry, _descriptionEditor, _errorLabel, buttons,
                },
            },
        };
    }

    private async Task SaveAsync()
    {
        if (_isSaving) return;
        var title = _titleEntry.Text?.Trim() ?? "";
        var description = _descriptionEditor.Text?.Trim() ?? "";
        if (title.Length == 0 || description.Length == 0)
        {
            ShowError(_strings.RequiredFields);
            return;
        }

        _errorLabel.IsVisible = false;
        SetSaving(true);
        try { await _onSave(title, description); }
        catch (OperationCanceledException) { ShowError(_strings.Error); }
        catch (Exception ex) { ShowError($"{_strings.Error}: {ex.Message}"); }
        finally { SetSaving(false); }
        // No fields are cleared here. A rejected or cancelled save retains the exact draft.
    }

    private void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.IsVisible = true;
    }

    private void SetSaving(bool saving)
    {
        _isSaving = saving;
        _saveButton.Text = saving ? _strings.Saving : _strings.Save;
        _saveButton.IsEnabled = _cancelButton.IsEnabled = !saving;
        _titleEntry.IsEnabled = _descriptionEditor.IsEnabled = !saving;
    }
}
