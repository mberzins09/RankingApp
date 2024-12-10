using RankingApp.ViewModels;
using System.Collections.ObjectModel;
using RankingApp.Models;
using static System.Net.Mime.MediaTypeNames;

namespace RankingApp.Views;

public partial class Games : ContentPage
{
    private readonly GameViewModel _viewModel;
    public Games(GameViewModel viewmodel)
	{
		InitializeComponent();
        _viewModel = viewmodel;
        BindingContext = _viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        PlayerSearchBar.Text = String.Empty;

        ListPlayers.ItemsSource = await _viewModel.GetPlayers();
    }

    private void PlayerSearchBar_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var search = sender == null ? String.Empty : ((SearchBar)sender).Text;

        var players = new ObservableCollection<PlayerDB>
            (_viewModel.SearchPlayers(_viewModel.PlayerList, search));

        ListPlayers.ItemsSource = players;
    }

    private async void ButtonGameSave_OnClicked(object? sender, EventArgs e)
    {
        await _viewModel.SaveGameAsync();
        await Shell.Current.GoToAsync(nameof(Tournaments));
    }

    private void EntrySets_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(entryMySets.Text))
        {
            _viewModel.MySets = 0;
        }
        if (string.IsNullOrEmpty(entryOpponentSets.Text))
        {
            _viewModel.OpponentSets = 0;
        }
        else
        {
            _viewModel.MySets = int.Parse(entryMySets.Text);
            _viewModel.OpponentSets = int.Parse(entryOpponentSets.Text);
            LabelRatingDifference.Text = RatingCalculator.Calculate(
                Data.TournamentPlayerPoints,
                _viewModel.GameOpponentPoints,
                _viewModel.MySets > _viewModel.OpponentSets,
                Data.Coefficient).ToString();
        }
    }

    private void ListPlayers_OnItemSelected(object? sender, SelectedItemChangedEventArgs e)
    {
        _viewModel.OpponentDb = ListPlayers.SelectedItem as PlayerDB;
        var opponent = _viewModel.OpponentDb;

        _viewModel.OpponentName = opponent.Name;
        _viewModel.OpponentSurname = opponent.Surname;
        _viewModel.GameOpponentPoints = opponent.Points;
        _viewModel.MySets = 0;
        _viewModel.OpponentSets = 0;
        labelSelectedOpponent.Text = $"{_viewModel.OpponentName} {_viewModel.OpponentSurname}";

        _viewModel.OpponentDb = null;
    }

    private void EntryOppName_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
            if (string.IsNullOrEmpty(EntryOppName.Text))
            {
                _viewModel.OpponentName = String.Empty;
            }
            if (string.IsNullOrEmpty(EntryOppSurname.Text))
            {
                _viewModel.OpponentSurname = String.Empty;
            }

            _viewModel.OpponentName = EntryOppName.Text;
            _viewModel.OpponentSurname = EntryOppSurname.Text;
            _viewModel.MySets = 0;
            _viewModel.OpponentSets = 0;
            _viewModel.GameOpponentPoints = 0;
            labelSelectedOpponent.Text = $"{_viewModel.OpponentName} {_viewModel.OpponentSurname}";
    }
}
