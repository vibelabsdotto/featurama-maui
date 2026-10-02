using System.Text.RegularExpressions;
using Featurama.Maui.UI.Strings;
using Featurama.Maui.UI.Theme;

namespace Featurama.Maui.UI.Views;

internal sealed class CreateRequestView : ContentView
{
    private readonly Entry _titleEntry;
    private readonly Editor _descEditor;
    private readonly Entry _emailEntry;
    private readonly Label _errorLabel;
    private readonly Button _submitBtn;
    private readonly Button _cancelBtn;
    private readonly FeaturamaStrings _strings;
    private readonly Func<string, string, string?, Task> _onSubmit;
    private readonly bool _emailRequired;
    private readonly bool _collectEmail;
    private bool _isSubmitting;

    public CreateRequestView(FeaturamaTheme theme, FeaturamaStrings strings, string emailCollection,
        Func<string, string, string?, Task> onSubmit, Action onCancel)
    {
        _strings = strings;
        _onSubmit = onSubmit;
        _emailRequired = emailCollection == "required";
        _collectEmail = _emailRequired || emailCollection == "optional";
        _titleEntry = new Entry
        {
            Placeholder = strings.TitlePlaceholder,
            PlaceholderColor = theme.TextSecondary,
            TextColor = theme.Text,
            BackgroundColor = theme.Secondary,
            FontSize = 16,
        };
        _descEditor = new Editor
        {
            Placeholder = strings.DescriptionPlaceholder,
            PlaceholderColor = theme.TextSecondary,
            TextColor = theme.Text,
            BackgroundColor = theme.Secondary,
            FontSize = 16,
            HeightRequest = 90,
        };
        _emailEntry = new Entry
        {
            Placeholder = _emailRequired ? strings.EmailRequired : strings.EmailOptional,
            PlaceholderColor = theme.TextSecondary,
            TextColor = theme.Text,
            BackgroundColor = theme.Secondary,
            Keyboard = Keyboard.Email,
            IsTextPredictionEnabled = false,
            IsVisible = _collectEmail,
        };
        _errorLabel = new Label { TextColor = theme.Text, IsVisible = false };
        _cancelBtn = new Button
        {
            Text = strings.Cancel,
            TextColor = theme.Text,
            BackgroundColor = theme.Secondary,
            CornerRadius = 8,
        };
        _cancelBtn.Clicked += (_, _) => { if (!_isSubmitting) onCancel(); };
        _submitBtn = new Button
        {
            Text = strings.Submit,
            TextColor = theme.AccentForeground,
            BackgroundColor = theme.Accent,
            CornerRadius = 8,
            FontAttributes = FontAttributes.Bold,
        };
        _submitBtn.Clicked += async (_, _) => await HandleSubmit();
        var buttons = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 12,
        };
        buttons.Add(_cancelBtn, 0);
        buttons.Add(_submitBtn, 1);
        Content = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = new SolidColorBrush(theme.BorderAccent),
            BackgroundColor = theme.Card,
            StrokeThickness = 1,
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 16),
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    _titleEntry, _descEditor, _emailEntry,
                    new Label { Text = strings.EmailHint, TextColor = theme.TextSecondary, FontSize = 12, IsVisible = _collectEmail },
                    _errorLabel, buttons,
                },
            },
        };
    }

    private async Task HandleSubmit()
    {
        if (_isSubmitting) return;
        var title = _titleEntry.Text?.Trim() ?? "";
        var description = _descEditor.Text?.Trim() ?? "";
        var email = _collectEmail ? _emailEntry.Text?.Trim() : null;
        if (title.Length == 0 || description.Length == 0)
        {
            ShowError(_strings.RequiredFields);
            return;
        }
        if ((_emailRequired || !string.IsNullOrEmpty(email)) &&
            (string.IsNullOrEmpty(email) || !Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$")))
        {
            ShowError(_strings.InvalidEmail);
            return;
        }

        _errorLabel.IsVisible = false;
        SetSubmitting(true);
        try
        {
            await _onSubmit(title, description, string.IsNullOrEmpty(email) ? null : email);
        }
        catch (OperationCanceledException)
        {
            ShowError(_strings.Error);
        }
        catch (Exception ex)
        {
            // Keep the draft intact; a failed HTTP call must not escape an async event handler.
            ShowError($"{_strings.Error}: {ex.Message}");
        }
        finally
        {
            SetSubmitting(false);
        }
    }

    private void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.IsVisible = true;
    }

    private void SetSubmitting(bool value)
    {
        _isSubmitting = value;
        _submitBtn.IsEnabled = _cancelBtn.IsEnabled = !value;
        _titleEntry.IsEnabled = _descEditor.IsEnabled = _emailEntry.IsEnabled = !value;
    }
}
