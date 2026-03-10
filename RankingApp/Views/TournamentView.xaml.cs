using RankingApp.ViewModels;

namespace RankingApp.Views;

public partial class TournamentView : BaseContentPage
{
    private readonly TournamentViewModel _viewModel;

    private bool _menuOpen = false;

    public TournamentView(TournamentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        MoreMenuPanel.SizeChanged += MoreMenuPanel_SizeChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
    }

    private void MoreMenuPanel_SizeChanged(object? sender, EventArgs e)
    {
        if (!_menuOpen && MoreMenuPanel.Width > 0)
        {
            MoreMenuPanel.TranslationX = MoreMenuPanel.Width;
        }
    }

    private async void MoreButton_Clicked(object sender, EventArgs e)
    {
        if (!_menuOpen)
        {
            MoreMenuOverlay.IsVisible = true;

            if (MoreMenuPanel.Width > 0)
                MoreMenuPanel.TranslationX = MoreMenuPanel.Width;

            await MoreMenuPanel.TranslateTo(0, 0, 250u, Easing.CubicOut);
            _menuOpen = true;
        }
        else
        {
            await CloseMenuAsync();
        }
    }

    async Task CloseMenuAsync()
    {
        if (!_menuOpen) return;
        await MoreMenuPanel.TranslateTo(MoreMenuPanel.Width, 0, 200, Easing.CubicIn);
        MoreMenuOverlay.IsVisible = false;
        _menuOpen = false;
    }

    private async void MoreOverlay_Tapped(object sender, EventArgs e)
    {
        await CloseMenuAsync();
    }

    private async void MenuAddGame_Clicked(object? sender, EventArgs e)
    {
        await CloseMenuAsync();
        await _viewModel.CreateNewGameAsync(false);
        await Shell.Current.GoToAsync(nameof(GameView));
    }

    private async void MenuAddDoubles_Clicked(object? sender, EventArgs e)
    {
        await CloseMenuAsync();
        await _viewModel.CreateNewGameAsync(true);
        await Shell.Current.GoToAsync(nameof(DoublesGameView));
    }

    private async void MenuSave_Clicked(object? sender, EventArgs e)
    {
        await CloseMenuAsync();
        await _viewModel.SaveTournamentAsync();
        await Shell.Current.GoToAsync(nameof(AllTournaments));
    }

    private void Entry_Focused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry)
        {
            if (entry.Text == "Enter Tournament Name")
            {
                entry.Text = string.Empty;
            }
        }
    }
}