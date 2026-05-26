using RankingApp.Core.Models;
using RankingApp.Core.ViewModels;

namespace RankingApp.Views;

public partial class GameView : BaseContentPage
{
    private readonly GameViewModel _viewModel;
    public GameView(GameViewModel viewmodel)
    {
        InitializeComponent();
        _viewModel = viewmodel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();

        _viewModel.SearchText = String.Empty;
    }

    private async void ButtonGameSave_OnClicked(object? sender, EventArgs e)
    {
        await _viewModel.SaveGameAsync();
        await Shell.Current.GoToAsync(nameof(TournamentView));
    }

    private async void EntryOppName_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var player = new PlayerDB
        {
            Name = EntryOpponentName.Text,
            Surname = EntryOpponentSurname.Text,
            Points = 0,
            PointsWithBonus = 0,
            Place = 0,
            BirthDate = ""
        };

        _viewModel.AssignOpponentProperties(player);

        await _viewModel.SaveGameAsync();
    }
}
