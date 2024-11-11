using RankingApp.Models;
using RankingApp.ViewModels;
using System.Collections.ObjectModel;

namespace RankingApp.Views;

public partial class AddTournament : ContentPage
{
    private readonly AddTournamentViewModel _viewModel;
	public AddTournament(AddTournamentViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        TournamentPlayerSearchBar.Text = String.Empty;

        AllPlayers.ItemsSource = await _viewModel.GetPlayers();
    }

    private async void TournamentDate_OnDateSelected(object? sender, DateChangedEventArgs e)
    {
        Data.TournamentDate = PickerTournamentDate.Date;
        await _viewModel.EditGamesDate(Data.TournamentDate);
    }

    private async void EntryTournamentName_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        Data.TournamentName = string.IsNullOrEmpty(EntryTournamentName.Text) ? String.Empty : EntryTournamentName.Text;
        await _viewModel.EditGamesTournamentName(Data.TournamentName);
    }

    private async void EntryTournamentCoefficient_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        Data.Coefficient = string.IsNullOrEmpty(EntryTournamentCoefficient.Text) ? 0f : float.Parse(EntryTournamentCoefficient.Text);
        await _viewModel.EditGamesCoefficient(Data.Coefficient);
    }

    private async void ButtonAdd_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Tournaments));
    }

    private void TournamentPlayerSearchBar_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var search = sender == null ? String.Empty : ((SearchBar)sender).Text;

        var players = new ObservableCollection<PlayerDB>
            (_viewModel.SearchPlayers(_viewModel.PlayerList, search));

        AllPlayers.ItemsSource = players;
    }

    private async void AllPlayers_OnItemSelected(object? sender, SelectedItemChangedEventArgs e)
    {
        var player = AllPlayers.SelectedItem as PlayerDB;
        Data.TournamentPlayerName = player.Name;
        Data.TournamentPlayerSurname = player.Surname;
        Data.TournamentPlayerPoints = player.Points;
        Data.TournamentPlayerId = player.Id;

        await _viewModel.EditGamesMe(
            Data.TournamentPlayerName, 
            Data.TournamentPlayerSurname,
            Data.TournamentPlayerPoints);
    }
}