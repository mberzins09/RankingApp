using RankingApp.Core.Models;
using RankingApp.Core.ViewModels;
using System.ComponentModel;

namespace RankingApp.Views;

public partial class EventResultsPage : BaseContentPage
{
    private readonly EventResultsViewModel _viewModel;

    // Drawn tables / brackets, built once per option and event - switching the picker only swaps the view
    private readonly Dictionary<string, View> _viewCache = [];
    private int _cacheVersion = -1;

    public EventResultsPage(EventResultsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadAsync();
        ShowSelected();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EventResultsViewModel.SelectedOption))
            ShowSelected();
    }

    private static Color ResourceColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color c ? c : fallback;

    private int _showToken;
    private string? _shownKey;
    private int _shownVersion = -1;

    private async void ShowSelected()
    {
        var option = _viewModel.SelectedOption;
        int token = ++_showToken;

        if (option == null || option.Kind == ResultViewKind.Participants)
        {
            DrawingScroll.Content = null;
            BusyIndicator.IsVisible = false;
            _shownKey = null;
            return;
        }

        if (_cacheVersion != _viewModel.DataVersion)
        {
            _viewCache.Clear();
            _cacheVersion = _viewModel.DataVersion;
        }

        // Already on screen (e.g. OnAppearing after the picker handled it)
        if (_shownKey == option.Key && _shownVersion == _cacheVersion && DrawingScroll.Content != null)
            return;

        _shownKey = null;

        if (_viewCache.TryGetValue(option.Key, out var cached))
        {
            DrawingScroll.Content = cached;
            BusyIndicator.IsVisible = false;
            _shownKey = option.Key;
            _shownVersion = _cacheVersion;
            return;
        }

        // Not built yet: clear the old drawing, show the spinner and let the UI paint it before the heavy work starts
        DrawingScroll.Content = null;
        BusyIndicator.IsVisible = true;
        await Task.Delay(50);

        if (token != _showToken)
            return;

        View? view = null;

        try
        {
            if (option.Kind == ResultViewKind.Stage)
            {
                // Whole stage: heading with the places it is played for, then every group / bracket under each other.
                // Parts are added one by one with a pause between them, so the page stays responsive and fills up gradually.
                var stack = new VerticalStackLayout { Spacing = 24, Padding = new Thickness(0, 0, 0, 24) };

                stack.Children.Add(new Label
                {
                    Text = option.Heading,
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ResourceColor("Accent", Colors.Orange)
                });

                DrawingScroll.Content = stack;

                foreach (var key in option.ItemKeys)
                {
                    View? part = null;

                    if (key.StartsWith("g:"))
                    {
                        var g = _viewModel.GetGroupTable(key);
                        if (g != null)
                            part = EventResultsDrawer.DrawGroupTable(g);
                    }
                    else
                    {
                        var b = _viewModel.GetBracket(key);
                        if (b != null)
                            part = EventResultsDrawer.DrawBracket(b);
                    }

                    if (part != null)
                    {
                        stack.Children.Add(part);
                        BusyIndicator.IsVisible = false; // first content is visible, the rest keeps arriving
                    }

                    await Task.Delay(1);

                    if (token != _showToken)
                        return; // another option was chosen meanwhile - drop this half-built page
                }

                view = stack;
            }
            else if (option.Kind == ResultViewKind.Group)
            {
                var group = _viewModel.GetGroupTable(option.Key);

                if (group != null)
                    view = EventResultsDrawer.DrawGroupTable(group);
            }
            else
            {
                var bracket = _viewModel.GetBracket(option.Key);

                if (bracket != null)
                    view = EventResultsDrawer.DrawBracket(bracket);
            }
        }
        finally
        {
            if (token == _showToken)
                BusyIndicator.IsVisible = false;
        }

        if (token != _showToken || view == null)
            return;

        _viewCache[option.Key] = view;
        _shownKey = option.Key;
        _shownVersion = _cacheVersion;
        DrawingScroll.Content = view;
    }
}
