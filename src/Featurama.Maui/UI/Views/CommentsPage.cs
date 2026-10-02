using Featurama.Maui.Models;
using Featurama.Maui.UI.Strings;
using Featurama.Maui.UI.Theme;

namespace Featurama.Maui.UI.Views;

internal sealed class CommentsPage : ContentPage
{
    private readonly FeatureRequest _request;
    private readonly FeaturamaClient _client;
    private readonly string _identity;
    private readonly FeaturamaTheme _theme;
    private readonly FeaturamaStrings _strings;
    private readonly VerticalStackLayout _comments = new() { Spacing = 12 };
    private readonly Editor _editor;
    private readonly Label _error;
    private readonly Button _post;
    private readonly Button _refresh;
    private readonly ActivityIndicator _loading;
    private List<Comment> _items = new();
    private CancellationTokenSource? _lifetime;
    private bool _busy;

    public CommentsPage(FeatureRequest request, string identity, FeaturamaTheme theme, FeaturamaStrings strings, FeaturamaClient? client = null)
    {
        _request = request;
        _client = client ?? Featurama.Client;
        _identity = identity;
        _theme = theme;
        _strings = strings;
        BackgroundColor = theme.Background;
        NavigationPage.SetHasNavigationBar(this, false);
        var close = new Button { Text = strings.Close, TextColor = theme.Accent, BackgroundColor = theme.AccentLight };
        close.Clicked += async (_, _) =>
        {
            close.IsEnabled = false;
            try { await Navigation.PopModalAsync(); }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { close.IsEnabled = true; }
        };
        _error = new Label { TextColor = theme.Text, IsVisible = false };
        _loading = new ActivityIndicator { Color = theme.Accent, IsVisible = false };
        _editor = new Editor
        {
            Placeholder = strings.CommentPlaceholder,
            PlaceholderColor = theme.TextSecondary,
            TextColor = theme.Text,
            BackgroundColor = theme.Secondary,
            HeightRequest = 100,
        };
        _post = new Button { Text = strings.AddComment, TextColor = theme.AccentForeground, BackgroundColor = theme.Accent };
        _post.Clicked += async (_, _) =>
        {
            var text = _editor.Text?.Trim();
            if (string.IsNullOrEmpty(text)) { ShowError(strings.CommentRequired); return; }
            await RunAsync(async token =>
            {
                await _client.AddCommentAsync(_request.Id, text, _identity, cancellationToken: token);
                _editor.Text = "";
                await LoadAsync(token);
            });
        };
        _refresh = new Button { Text = strings.Refresh, TextColor = theme.Accent, BackgroundColor = theme.AccentLight };
        _refresh.Clicked += async (_, _) => await RunAsync(LoadAsync);
        var body = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 12,
            Children =
            {
                new Label { Text = request.Title, TextColor = theme.Text, FontSize = 22, FontAttributes = FontAttributes.Bold },
                new Label { Text = request.Description, TextColor = theme.TextSecondary },
                new Label { Text = strings.Pending, TextColor = theme.Accent, IsVisible = !request.IsApproved },
                new Label { Text = strings.Comments, TextColor = theme.Text, FontSize = 18, FontAttributes = FontAttributes.Bold },
                _error, _loading, _refresh, _comments, _editor, _post,
            },
        };
        var grid = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
        };
        grid.Add(close, 0, 0);
        grid.Add(new ScrollView { Content = body }, 0, 1);
        Content = grid;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _lifetime?.Dispose();
        _lifetime = new CancellationTokenSource();
        await RunAsync(LoadAsync);
    }

    protected override void OnDisappearing()
    {
        _lifetime?.Cancel();
        base.OnDisappearing();
    }

    private async Task LoadAsync(CancellationToken token)
    {
        var comments = await _client.GetCommentsAsync(_request.Id, token);
        token.ThrowIfCancellationRequested();
        _items = comments.ToList();
        _request.CommentCount = _items.Count;
        RebuildComments();
    }

    private void RebuildComments()
    {
        _comments.Children.Clear();
        if (_items.Count == 0 && !_busy)
            _comments.Children.Add(new Label { Text = _strings.NoComments, TextColor = _theme.TextSecondary });
        foreach (var comment in _items)
        {
            var author = string.IsNullOrWhiteSpace(comment.AuthorName) ? _strings.Anonymous : comment.AuthorName;
            if (comment.AuthorRole == "developer") author = $"{author} · {_strings.Team}";
            var vote = new Button
            {
                // The public comment response has no hasVoted field. Do not invent a selected state.
                Text = $"{_strings.ToggleCommentVote} · {comment.VoteCount}",
                TextColor = _theme.Accent,
                BackgroundColor = _theme.AccentLight,
                HorizontalOptions = LayoutOptions.Start,
                IsEnabled = !_busy,
                FontSize = 12,
            };
            vote.Clicked += async (_, _) => await RunAsync(async token =>
            {
                var updated = await _client.ToggleCommentVoteAsync(_request.Id, comment.Id, _identity, token);
                comment.VoteCount = updated.VoteCount;
                await LoadAsync(token);
            });
            _comments.Children.Add(new Border
            {
                Stroke = new SolidColorBrush(_theme.Border),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                BackgroundColor = _theme.Card,
                Padding = 12,
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new Label { Text = author, TextColor = _theme.Text, FontAttributes = FontAttributes.Bold },
                        new Label { Text = comment.CreatedAt.ToLocalTime().ToString("g"), TextColor = _theme.TextSecondary, FontSize = 12 },
                        new Label { Text = comment.Content, TextColor = _theme.Text, LineBreakMode = LineBreakMode.WordWrap },
                        vote,
                    },
                },
            });
        }
    }

    private async Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (_busy) return;
        var token = _lifetime?.Token ?? CancellationToken.None;
        _busy = true;
        _error.IsVisible = false;
        SetBusy(true);
        try { await action(token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) { ShowError($"{_strings.Error}: {ex.Message}"); }
        finally
        {
            _busy = false;
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _post.IsEnabled = _refresh.IsEnabled = _editor.IsEnabled = !busy;
        _loading.IsVisible = _loading.IsRunning = busy;
        RebuildComments();
    }

    private void ShowError(string text)
    {
        _error.Text = text;
        _error.IsVisible = true;
    }
}
