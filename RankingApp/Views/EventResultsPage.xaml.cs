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

    private void ShowSelected()
    {
        var option = _viewModel.SelectedOption;

        if (option == null || option.Kind == ResultViewKind.Participants)
        {
            DrawingScroll.Content = null;
            return;
        }

        if (_cacheVersion != _viewModel.DataVersion)
        {
            _viewCache.Clear();
            _cacheVersion = _viewModel.DataVersion;
        }

        if (!_viewCache.TryGetValue(option.Key, out var view))
        {
            view = null;

            if (option.Kind == ResultViewKind.Stage)
            {
                // Whole stage: heading with the places it is played for, then every group / bracket under each other
                var stack = new VerticalStackLayout { Spacing = 24, Padding = new Thickness(0, 0, 0, 24) };

                stack.Children.Add(new Label
                {
                    Text = option.Heading,
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ResourceColor("Accent", Colors.Orange)
                });

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
                        stack.Children.Add(part);
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

            if (view == null)
            {
                DrawingScroll.Content = null;
                return;
            }

            _viewCache[option.Key] = view;
        }

        DrawingScroll.Content = view;
    }
}
