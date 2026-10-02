using Featurama.Maui.Models;
using Featurama.Maui.UI.Strings;
using Featurama.Maui.UI.Theme;
using Featurama.Maui.UI.Utils;
using Featurama.Maui.UI.Views;

namespace Featurama.Maui.UI;

public sealed class FeaturamaPage : ContentPage
{
    private const int PageSize = 20;
    private readonly FeaturamaClient _client;
    private readonly FeaturamaTheme _theme;
    private readonly FeaturamaStrings _strings;
    private readonly string _voterId;
    private readonly Models.DeviceInfo? _deviceInfoOverride;
    private readonly ScrollView _scroll;
    private readonly VerticalStackLayout _listContainer = new();
    private readonly VerticalStackLayout _formContainer = new();
    private readonly BrandingView _brandingView;
    private readonly FilterTabsView _filterTabs;
    private readonly HashSet<Guid> _votingIds = new();
    private CreateRequestView? _createForm;
    private EditRequestView? _editForm;
    private FeatureRequest? _editingRequest;
    private Guid? _savingRequestId;
    private ProjectConfig? _config;
    private PaginatedResponse<FeatureRequest>? _data;
    private CancellationTokenSource? _lifetime;
    private CancellationTokenSource? _loadCancellation;
    private string _activeFilter = "new";
    private string? _error;
    private string? _configError;
    private string? _notice;
    private int _configRequests;
    private int _page = 1;
    private int _loadVersion;
    private bool _isLoading;
    private bool _openingComments;

    public FeaturamaPage(FeaturamaPageOptions? options = null)
    {
        var opts = options ?? new FeaturamaPageOptions();
        _client = Featurama.Client; // Bind drafts to the project/credentials that opened this page.
        _theme = ThemeFactory.Create(opts.AccentColor ?? Color.FromArgb("#007AFF"), opts.ColorScheme);
        _strings = opts.Strings ?? new FeaturamaStrings();
        // Keep the identity local and immutable. Another page/account must not change
        // the owner used by this page's list, drafts, votes or pending HTTP calls.
        _voterId = string.IsNullOrWhiteSpace(opts.SubmitterIdentifier)
            ? VoterIdProvider.GetOrCreate() : opts.SubmitterIdentifier;
        _deviceInfoOverride = opts.DeviceInfo == null ? null : DeviceMetadataProvider.GetDeviceInfo(opts.DeviceInfo);
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = _theme.Background;

        Func<Task>? onClose = opts.OnCloseAsync;
        if (onClose == null && opts.OnClose != null)
            onClose = () => { opts.OnClose(); return Task.CompletedTask; };
        var header = new HeaderView(_theme, _strings, onClose, OnAddAsync, ShowError);
        _filterTabs = new FilterTabsView(_theme, _strings);
        _filterTabs.FilterChanged += async (_, filter) =>
        {
            if (_activeFilter == filter) return;
            _activeFilter = filter;
            await RunSafelyAsync(() => LoadDataAsync(1, clear: true));
        };
        _brandingView = new BrandingView(_theme) { Margin = new Thickness(0, 8, 0, 12) };
        _scroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 12),
                Children = { _formContainer, _listContainer },
            },
        };
        // Keep the form scrollable and branding out of the list's hit targets.
        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
            },
        };
        grid.Add(new VerticalStackLayout { Children = { header, _filterTabs } }, 0, 0);
        grid.Add(_scroll, 0, 1);
        grid.Add(_brandingView, 0, 2);
        Content = grid;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _lifetime?.Dispose();
        _lifetime = new CancellationTokenSource();
        await RunSafelyAsync(RefreshAsync);
    }

    protected override void OnDisappearing()
    {
        _lifetime?.Cancel();
        _loadCancellation?.Cancel();
        _loadVersion++;
        _isLoading = false;
        base.OnDisappearing();
    }

    private async Task RefreshAsync()
    {
        var token = _lifetime?.Token ?? CancellationToken.None;
        await LoadConfigAsync(token);
        if (!token.IsCancellationRequested)
            await LoadDataAsync(_page);
    }

    private async Task LoadConfigAsync(CancellationToken token)
    {
        _configRequests++;
        RebuildList();
        try
        {
            var config = await _client.GetProjectConfigAsync(token);
            token.ThrowIfCancellationRequested();
            _config = config;
            _configError = null;
            _brandingView.IsVisible = config.Branding.ShowBranding;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                _config = null;
                _configError = $"{_strings.ConfigError} {ex.Message}";
            }
        }
        finally
        {
            _configRequests--;
            RebuildList();
        }
    }

    private async Task OnAddAsync()
    {
        if (_createForm != null) return;
        var token = _lifetime?.Token ?? CancellationToken.None;
        // Fetch the current email policy before offering a submission form.
        await LoadConfigAsync(token);
        if (_config == null || token.IsCancellationRequested || _createForm != null) return;
        _createForm = new CreateRequestView(_theme, _strings, _config.EmailCollection,
            OnSubmitRequestAsync, RemoveCreateForm);
        _formContainer.Children.Add(_createForm);
    }

    private void RemoveCreateForm()
    {
        if (_createForm != null) _formContainer.Children.Remove(_createForm);
        _createForm = null;
    }

    private async Task OnSubmitRequestAsync(string title, string description, string? email)
    {
        var token = _lifetime?.Token ?? CancellationToken.None;
        var created = await _client.CreateFeatureRequestAsync(new CreateFeatureRequestInput
        {
            Title = title,
            Description = description,
            SubmitterIdentifier = _voterId,
            Email = email,
            DeviceInfo = DeviceMetadataProvider.GetDeviceInfo(_deviceInfoOverride),
        }, token);
        RemoveCreateForm();
        _notice = created.IsApproved ? _strings.Submitted : _strings.SubmittedPending;
        _activeFilter = "new";
        _filterTabs.SetFilter("new");
        if (!token.IsCancellationRequested)
            await LoadDataAsync(1, clear: true);
    }

    private bool IsOwner(FeatureRequest request) =>
        !string.IsNullOrWhiteSpace(request.SubmitterIdentifier) &&
        string.Equals(request.SubmitterIdentifier, _voterId, StringComparison.Ordinal);

    private async Task OnEditAsync(FeatureRequest request)
    {
        if (!IsOwner(request)) throw new InvalidOperationException(_strings.EditOwnerOnly);
        if (_editForm != null)
        {
            if (_editingRequest?.Id != request.Id)
                throw new InvalidOperationException(_strings.EditInProgress);
        }
        else
        {
            _editingRequest = request;
            _editForm = new EditRequestView(_theme, _strings, request,
                (title, description) => OnSaveRequestAsync(request, title, description), RemoveEditForm);
            // The draft lives outside the list, so refresh/filter/vote operations cannot erase it.
            _formContainer.Children.Insert(0, _editForm);
            _notice = null;
            RebuildList();
        }
        if (_scroll.Handler != null) await _scroll.ScrollToAsync(0, 0, true);
    }

    private void RemoveEditForm()
    {
        if (_editForm != null) _formContainer.Children.Remove(_editForm);
        _editForm = null;
        _editingRequest = null;
    }

    private async Task OnSaveRequestAsync(FeatureRequest request, string title, string description)
    {
        // Check again in case a refresh changed the visible record while the draft was open.
        var current = _data?.Items.FirstOrDefault(item => item.Id == request.Id) ?? request;
        if (!IsOwner(request) || !IsOwner(current))
            throw new InvalidOperationException(_strings.EditOwnerOnly);
        if (_votingIds.Contains(request.Id) || _savingRequestId != null)
            throw new InvalidOperationException(_strings.RequestBusy);
        var token = _lifetime?.Token ?? CancellationToken.None;
        token.ThrowIfCancellationRequested();
        _savingRequestId = request.Id;
        RebuildList();
        try
        {
            var updated = await _client.UpdateFeatureRequestAsync(request.Id, title, description,
                submitterIdentifier: _voterId, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            if (updated.Id != request.Id)
                throw new InvalidOperationException(_strings.Error);

            // A list request started before the save must not overwrite the saved text.
            _loadCancellation?.Cancel();
            _loadVersion++;
            _isLoading = false;
            ApplyEdit(request, updated);
            var visible = _data?.Items.FirstOrDefault(item => item.Id == request.Id);
            if (visible != null && !ReferenceEquals(visible, request)) ApplyEdit(visible, updated);
            RemoveEditForm();
            _notice = updated.IsApproved ? _strings.Saved : _strings.SavedPending;
            RebuildList();
            // Keep the current filter/page. A read failure has its own retry and must
            // not claim the successful mutation failed or ask the user to resubmit it.
            await LoadDataAsync(_page);
        }
        finally
        {
            _savingRequestId = null;
            RebuildList();
        }
    }

    private static void ApplyEdit(FeatureRequest target, FeatureRequest updated)
    {
        target.Title = updated.Title;
        target.Description = updated.Description;
        target.Status = updated.Status;
        target.Source = updated.Source;
        target.IsApproved = updated.IsApproved;
        target.VoteCount = updated.VoteCount;
        target.CommentCount = updated.CommentCount;
        // Update responses can omit hasVoted and redact identity/contact/device fields.
        // Keep those until the identity-scoped list refresh supplies authoritative state.
    }

    private async Task LoadDataAsync(int page, bool clear = false)
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime?.Token ?? CancellationToken.None);
        var token = _loadCancellation.Token;
        var version = ++_loadVersion;
        var filter = _activeFilter;
        _page = page;
        _isLoading = true;
        _error = null;
        if (clear) _data = null;
        RebuildList();
        try
        {
            var data = await _client.GetFeatureRequestsAsyncForUser(_voterId,
                page: page, pageSize: PageSize, filter: filter, cancellationToken: token);
            if (token.IsCancellationRequested || version != _loadVersion) return;
            // Moderation can shrink the result set while a later page is open.
            if (page > 1 && data.Items.Count == 0)
            {
                await LoadDataAsync(Math.Max(1, Math.Min(page - 1, data.TotalPages)), clear: true);
                return;
            }
            _data = data;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (version == _loadVersion) _error = $"{_strings.Error}: {ex.Message}";
        }
        finally
        {
            if (version == _loadVersion)
            {
                _isLoading = false;
                RebuildList();
            }
        }
    }

    private void RebuildList()
    {
        _listContainer.Children.Clear();
        if (_notice != null) _listContainer.Children.Add(Message(_notice));
        if (_configError != null)
        {
            _listContainer.Children.Add(Message(_configError));
            _listContainer.Children.Add(ActionButton(_strings.Retry, RefreshAsync));
        }
        if (_error != null)
        {
            _listContainer.Children.Add(Message(_error));
            _listContainer.Children.Add(ActionButton(_strings.Retry, () => LoadDataAsync(_page)));
        }
        if (_isLoading || _configRequests > 0)
            _listContainer.Children.Add(new ActivityIndicator { IsRunning = true, Color = _theme.Accent, Margin = new Thickness(0, 12) });
        if (_data != null)
        {
            foreach (var request in _data.Items)
            {
                _listContainer.Children.Add(new RequestCardView(_theme, _strings, request,
                    _isLoading || _votingIds.Contains(request.Id) || _savingRequestId == request.Id,
                    () => HandleVoteAsync(request), () => OpenCommentsAsync(request), ShowError,
                    IsOwner(request) ? () => OnEditAsync(request) : null));
            }
            if (_data.Items.Count == 0 && !_isLoading)
            {
                _listContainer.Children.Add(Message(_strings.Empty));
                _listContainer.Children.Add(Message(_strings.EmptyHint));
            }
            var pagination = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                },
                ColumnSpacing = 8,
                Margin = new Thickness(0, 8),
            };
            var previous = ActionButton(_strings.Previous, () => LoadDataAsync(_page - 1, clear: true));
            previous.IsEnabled = !_isLoading && _data.HasPreviousPage;
            var next = ActionButton(_strings.Next, () => LoadDataAsync(_page + 1, clear: true));
            next.IsEnabled = !_isLoading && _data.HasNextPage;
            pagination.Add(previous, 0);
            pagination.Add(new Label
            {
                Text = string.Format(_strings.PageFormat, _data.Page, Math.Max(1, _data.TotalPages)),
                TextColor = _theme.TextSecondary,
                VerticalTextAlignment = TextAlignment.Center,
                FontSize = 12,
            }, 1);
            pagination.Add(next, 2);
            _listContainer.Children.Add(pagination);
        }
        if (_data == null && _page > 1)
            _listContainer.Children.Add(ActionButton(_strings.Previous, () => LoadDataAsync(_page - 1, clear: true)));
        _listContainer.Children.Add(ActionButton(_strings.Refresh, RefreshAsync));
    }

    private Label Message(string text) => new()
    {
        Text = text, TextColor = _theme.TextSecondary, Margin = new Thickness(0, 8),
    };

    private Button ActionButton(string text, Func<Task> action)
    {
        var button = new Button
        {
            Text = text,
            TextColor = _theme.Accent,
            BackgroundColor = _theme.AccentLight,
            CornerRadius = 8,
            IsEnabled = !_isLoading && _configRequests == 0,
        };
        button.Clicked += async (_, _) =>
        {
            button.IsEnabled = false;
            try { await RunSafelyAsync(action); }
            finally { button.IsEnabled = !_isLoading; }
        };
        return button;
    }

    private async Task HandleVoteAsync(FeatureRequest request)
    {
        if (!request.IsApproved || _savingRequestId == request.Id || !_votingIds.Add(request.Id)) return;
        var token = _lifetime?.Token ?? CancellationToken.None;
        RebuildList();
        try
        {
            var wasVoted = request.HasVoted;
            var updated = wasVoted
                ? await _client.RemoveVoteAsync(request.Id, _voterId, token)
                : await _client.VoteAsync(request.Id, _voterId, token);
            request.VoteCount = updated.VoteCount;
            request.HasVoted = !wasVoted;
            if (!token.IsCancellationRequested) await LoadDataAsync(_page);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) { ShowError(ex); }
        finally
        {
            _votingIds.Remove(request.Id);
            RebuildList();
        }
    }

    private async Task OpenCommentsAsync(FeatureRequest request)
    {
        if (_openingComments) return;
        _openingComments = true;
        try
        {
            await Navigation.PushModalAsync(new NavigationPage(new CommentsPage(request, _voterId, _theme, _strings, _client)));
        }
        finally { _openingComments = false; }
    }

    private async Task RunSafelyAsync(Func<Task> action)
    {
        try { await action(); }
        catch (OperationCanceledException) when (_lifetime?.IsCancellationRequested == true) { }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ShowError(Exception ex)
    {
        _error = $"{_strings.Error}: {ex.Message}";
        RebuildList();
    }
}
