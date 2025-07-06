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
        await _viewModel.LoadDataAsync();
        TournamentPlayerSearchBar.Text = String.Empty;

        AllPlayers.ItemsSource = await _viewModel.GetPlayers();
    }

    private async void TournamentDate_OnDateSelected(object? sender, DateChangedEventArgs e)
    {
        await _viewModel.EditDate(_viewModel.Tournament.Date);
    }

    private async void EntryTournamentName_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        await _viewModel.EditTournamentName(_viewModel.Tournament.Name);
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
        if (player != null)
        {
            _viewModel.Tournament.TournamentPlayerName = player.Name;
            _viewModel.Tournament.TournamentPlayerSurname = player.Surname;
            _viewModel.Tournament.TournamentPlayerPoints = player.Points;
        }

        await _viewModel.EditMe(
            _viewModel.Tournament.TournamentPlayerName,
            _viewModel.Tournament.TournamentPlayerSurname,
            _viewModel.Tournament.TournamentPlayerPoints);
    }

    private async void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        await _viewModel.EditCoefficient(_viewModel.Tournament.Coefficient);
    }
}