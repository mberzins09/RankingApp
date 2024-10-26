using RankingApp.Controllers;

namespace RankingApp.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
    }

    private void BtnWomensRank_OnClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync(nameof(WomensRanking));
    }

    private void BtnMensRank_OnClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync(nameof(MensRanking));
    }

    private void BtnAddTournament_OnClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync(nameof(AddTournament));
    }

    private void BtnEditTournament_OnClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync(nameof(AllTournaments));
    }

    private void BtnTournamentList_OnClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync(nameof(AllTournaments));
    }
}