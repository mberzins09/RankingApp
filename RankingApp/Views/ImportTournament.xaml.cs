using RankingApp.Core.ViewModels;

namespace RankingApp.Views;

public partial class ImportTournament : BaseContentPage
{
    private readonly ImportTournamentViewModel _importTournamentViewModel;

    public ImportTournament(ImportTournamentViewModel importTournamentViewModel)
    {
        InitializeComponent();

        _importTournamentViewModel = importTournamentViewModel;
        BindingContext = _importTournamentViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _importTournamentViewModel.LoadTournaments();
    }
}
