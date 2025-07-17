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

        _viewModel.SearchText = String.Empty;
    }

    private async void ButtonGameSave_OnClicked(object? sender, EventArgs e)
    {
        await _viewModel.SaveGameAsync();
        await Shell.Current.GoToAsync(nameof(Tournaments));
    }

    private async void EntryOppName_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        _viewModel.OneGame.Name = EntryOpponentName.Text;
        _viewModel.OneGame.Surname = EntryOpponentSurname.Text;
        _viewModel.OneGame.OpponentPoints = 0;

        await _viewModel.SaveGameAsync();
    }
}
