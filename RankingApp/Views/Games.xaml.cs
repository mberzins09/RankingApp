using RankingApp.ViewModels;
using System.Collections.ObjectModel;
using RankingApp.Models;

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
        await _viewModel.LoadDataAsync();
        if (_viewModel.Game != null)
        {
            LabelOpponentName.Text = _viewModel.Game.Name;
            LabelOpponentSurname.Text = _viewModel.Game.Surname;
            LabelMyName.Text = _viewModel.Game.MyName;
            LabelMySurname.Text = _viewModel.Game.MySurname;
            if (_viewModel.Game.MySets != null && _viewModel.Game.OpponentSets != null)
            {
                MySetsPicker.SelectedItem = _viewModel.Game.MySets;
                OpponentSetsPicker.SelectedItem = _viewModel.Game.OpponentSets;
                IsForeignPicker.SelectedItem = _viewModel.Game.IsOpponentForeign ? "Yes" : "No";
            }
            
            UpdateRatingDifference();
            if (_viewModel.Game.MySets == null || _viewModel.Game.OpponentSets == null || _viewModel.Game.Name == null)
            {
                LabelRatingDifference.Text = "0";
            }
        }

        PlayerSearchBar.Text = String.Empty;
        ListPlayers.ItemsSource = await _viewModel.GetPlayers();
    }

    private async void OnMySetsChanged(object sender, EventArgs e)
    {
        if (MySetsPicker.SelectedItem is int selectedValue)
        {
            _viewModel.Game.MySets = selectedValue;
            await HandleRatingAndSaveAsync();
        }
    }

    private async void OnOpponentSetsChanged(object sender, EventArgs e)
    {
        if (OpponentSetsPicker.SelectedItem is int selectedValue)
        {
            _viewModel.Game.OpponentSets = selectedValue;
            await HandleRatingAndSaveAsync();
        }
    }

    private async void IsForeignPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (IsForeignPicker.SelectedItem is string selection)
        {
            _viewModel.Game.IsOpponentForeign = selection == "Yes";
            UpdateRatingDifference();
            await _viewModel.SaveGameAsync();
        }
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

    private async void ListPlayers_OnItemSelected(object? sender, SelectedItemChangedEventArgs e)
    {
        var opponent = ListPlayers.SelectedItem as PlayerDB;

        if (opponent != null)
        {
            _viewModel.Game.Name = opponent.Name;
            _viewModel.Game.Surname = opponent.Surname;
            _viewModel.Game.OpponentPoints = opponent.Points;
            UpdateRatingDifference();
            if (_viewModel.Game.MySets == null || _viewModel.Game.OpponentSets == null)
            {
                LabelRatingDifference.Text = "0";
            }
            await _viewModel.SaveGameAsync();
        }

        LabelOpponentName.Text = _viewModel.Game.Name;
        LabelOpponentSurname.Text = _viewModel.Game.Surname;
    }

    private async void EntryOppName_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        _viewModel.Game.Name = EntryOpponentName.Text;
        _viewModel.Game.Surname = EntryOpponentSurname.Text;
        _viewModel.Game.OpponentPoints = 0;

        await _viewModel.SaveGameAsync();

        LabelOpponentName.Text = _viewModel.Game.Name;
        LabelOpponentSurname.Text = _viewModel.Game.Surname;
    }

    private async Task HandleRatingAndSaveAsync()
    {
        UpdateRatingDifference();

        if (_viewModel.Game.MySets == null ||
            _viewModel.Game.OpponentSets == null ||
            string.IsNullOrEmpty(_viewModel.Game.Name))
        {
            LabelRatingDifference.Text = "0";
        }

        await _viewModel.SaveGameAsync();
    }

    private void UpdateRatingDifference()
    {
        if (!_viewModel.Game.IsOpponentForeign)
        {
            LabelRatingDifference.Text = RatingCalculator.Calculate(
                _viewModel.Game.MyPoints,
                _viewModel.Game.OpponentPoints,
                _viewModel.Game.MySets > _viewModel.Game.OpponentSets,
                _viewModel.Game.GameCoefficient).ToString();
        }
        else
        {
            LabelRatingDifference.Text = "0";
        }
    }
}
