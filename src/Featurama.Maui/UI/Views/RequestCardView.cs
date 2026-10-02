using Featurama.Maui.Models;
using Featurama.Maui.UI.Strings;
using Featurama.Maui.UI.Theme;

namespace Featurama.Maui.UI.Views;

internal sealed class RequestCardView : ContentView
{
    public RequestCardView(FeaturamaTheme theme, FeaturamaStrings strings, FeatureRequest request,
        bool isBusy, Func<Task> onToggleVote, Func<Task> onComments, Action<Exception> onError,
        Func<Task>? onEdit = null)
    {
        var voteBtn = new Button
        {
            Text = $"{(request.HasVoted ? "\u2713" : "\u25B2")}\n{request.VoteCount}",
            TextColor = request.HasVoted ? theme.AccentForeground : theme.Accent,
            BackgroundColor = request.HasVoted ? theme.Accent : theme.AccentLight,
            CornerRadius = 8,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            WidthRequest = 56,
            HeightRequest = 60,
            IsEnabled = request.IsApproved && !isBusy,
            Padding = new Thickness(4),
            VerticalOptions = LayoutOptions.Start,
        };
        SemanticProperties.SetDescription(voteBtn,
            $"{(!request.IsApproved ? strings.Pending : request.HasVoted ? strings.RemoveVote : strings.Vote)}, {request.VoteCount}");
        voteBtn.Clicked += async (_, _) =>
        {
            try { await onToggleVote(); }
            catch (Exception ex) { onError(ex); }
        };

        var content = new VerticalStackLayout { Spacing = 6 };
        content.Children.Add(new Label
        {
            Text = request.Title,
            TextColor = theme.Text,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.WordWrap,
        });
        if (!request.IsApproved || request.Status == FeatureRequestStatus.Roadmap)
        {
            content.Children.Add(new Label
            {
                Text = !request.IsApproved ? strings.Pending : strings.BadgePlanned,
                TextColor = theme.Accent,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
            });
        }
        if (request.HasVoted)
            content.Children.Add(new Label { Text = strings.Voted, TextColor = theme.Accent, FontSize = 12 });
        if (!string.IsNullOrEmpty(request.Description))
        {
            content.Children.Add(new Label
            {
                Text = request.Description,
                TextColor = theme.TextSecondary,
                FontSize = 13,
                MaxLines = 3,
                LineBreakMode = LineBreakMode.TailTruncation,
            });
        }
        var comments = new Button
        {
            Text = $"{strings.Comments} ({request.CommentCount})",
            TextColor = theme.Accent,
            BackgroundColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Start,
            Padding = new Thickness(0, 4),
            FontSize = 13,
            IsEnabled = !isBusy,
        };
        comments.Clicked += async (_, _) =>
        {
            comments.IsEnabled = false;
            try { await onComments(); }
            catch (Exception ex) { onError(ex); }
            finally { comments.IsEnabled = !isBusy; }
        };
        content.Children.Add(comments);
        // No edit control at all for requests not owned by this page's identity.
        if (onEdit != null)
        {
            var edit = new Button
            {
                Text = strings.Edit,
                TextColor = theme.Accent,
                BackgroundColor = theme.AccentLight,
                HorizontalOptions = LayoutOptions.Start,
                FontSize = 13,
                CornerRadius = 8,
                IsEnabled = !isBusy,
            };
            SemanticProperties.SetDescription(edit, $"{strings.Edit}: {request.Title}");
            edit.Clicked += async (_, _) =>
            {
                edit.IsEnabled = false;
                try { await onEdit(); }
                catch (Exception ex) { onError(ex); }
                finally { edit.IsEnabled = !isBusy; }
            };
            content.Children.Add(edit);
        }
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 12,
        };
        grid.Add(voteBtn, 0);
        grid.Add(content, 1);
        Content = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = new SolidColorBrush(theme.Border),
            BackgroundColor = theme.Card,
            StrokeThickness = 1,
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12),
            Content = grid,
        };
    }
}
