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
        await _viewModel.Migrate();
        await _viewModel.LoadDataAsync();
        _viewModel.SearchText = String.Empty;
    }

    private async void ImageButtonAdd_Clicked(object sender, EventArgs e)
    {
        await _viewModel.CreateNewTournamentSave();
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