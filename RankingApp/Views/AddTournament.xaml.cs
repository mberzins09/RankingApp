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

    private void TournamentDate_OnDateSelected(object? sender, DateChangedEventArgs e)
    {
        Data.TournamentDate = PickerTournamentDate.Date;
    }

    private void EntryTournamentName_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        Data.TournamentName = string.IsNullOrEmpty(EntryTournamentName.Text) ? String.Empty : EntryTournamentName.Text;
    }

    private void EntryTournamentCoefficient_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        Data.Coefficient = string.IsNullOrEmpty(EntryTournamentCoefficient.Text) ? 0f : float.Parse(EntryTournamentCoefficient.Text);
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

    private void AllPlayers_OnItemSelected(object? sender, SelectedItemChangedEventArgs e)
    {
        var player = AllPlayers.SelectedItem as PlayerDB;
        Data.TournamentPlayerName = player.Name;
        Data.TournamentPlayerSurname = player.Surname;
        Data.TournamentPlayerPoints = player.Points;
    }
}