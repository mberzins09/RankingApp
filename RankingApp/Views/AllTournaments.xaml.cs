using RankingApp.ViewModels;
using System.Collections.ObjectModel;
using RankingApp.Models;
using RankingApp.Services;

namespace RankingApp.Views;

public partial class AllTournaments : ContentPage
{
    private readonly AllTournamentsViewModel _viewModel;
    private readonly DatabaseService _databaseService;
    public AllTournaments(AllTournamentsViewModel viewModel, DatabaseService databaseService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _databaseService = databaseService;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
        TournamentsSearchBar.Text = String.Empty;
    }

    private void TournamentsSearchBar_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        _viewModel.FilterTournaments(e.NewTextValue);
    }

    private void ListViewTournaments_OnItemSelected(object? sender, SelectedItemChangedEventArgs e)
    {
        var tournament = ListViewTournaments.SelectedItem as Tournament;
        if (tournament != null)
        {
            Data.TournamentId = tournament.Id;
        }

        Shell.Current.GoToAsync(nameof(Tournaments));
    }

    private async void MenuItemDelete_OnClicked(object? sender, EventArgs e)
    {
        var menuItem = sender as MenuItem;
        if (menuItem != null)
        {
            var tournament = menuItem.CommandParameter as Tournament;
            if (tournament != null)
            {
                var games = await _viewModel.GetGames(tournament.Id);
                for (int i = 0; i < games.Count; i++)
                {
                    await _viewModel.DeleteGame(games[i]);
                }
                await _viewModel.DeleteTournament(tournament);
            }
        }

        ListViewTournaments.ItemsSource = await _viewModel.GetTournaments();
    }

    private async void MenuItemEdit_OnClicked(object? sender, EventArgs e)
    {
        var menuItem = sender as MenuItem;
        if (menuItem != null)
        {
            var tournament = menuItem.CommandParameter as Tournament;
            if (tournament != null)
            {
                Data.TournamentId = tournament.Id;

                await Shell.Current.GoToAsync(nameof(AddTournament));
            }
        }
    }

    private async void ImageButtonAdd_Clicked(object sender, EventArgs e)
    {
        var player = new PlayerDB()
        {
            Id = 10000,
            Place = 10000,
            Points = 0,
            PointsWithBonus = 0,
            Name = "Name",
            Surname = "Surname",
            Gender = "male",
            OverallPlace = 10000,
            BirthDate = ""
        };
        var playerDB = await _databaseService.GetPlayerAsync(694);
        if (playerDB != null)
        {
            player.Id = playerDB.Id;
            player.Place = playerDB.Place;
            player.Points = playerDB.Points;
            player.PointsWithBonus = playerDB.PointsWithBonus;
            player.Name = playerDB.Name;
            player.Surname = playerDB.Surname;
            player.Gender = playerDB.Gender;
            player.OverallPlace = playerDB.OverallPlace;
            player.BirthDate = playerDB.BirthDate;
        }
        var tournament = new Tournament()
        {
            Coefficient = "0.5",
            Name = "New Tournament",
            Date = DateTime.Now,
            TournamentPlayerName = player.Name,
            TournamentPlayerSurname = player.Surname,
            TournamentPlayerPoints = player.Points,
            TournamentPlayerId = player.Id
        };
        await _databaseService.SaveTournamentAsync(tournament);
        Data.TournamentId = tournament.Id;
        await Shell.Current.GoToAsync(nameof(AddTournament));
    }

    private async void ImageButtonAllGames_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AllGames));
    }

    private async void ImageButtonRankings_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AllPlayerRanking));
    }
}