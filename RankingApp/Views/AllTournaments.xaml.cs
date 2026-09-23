using RankingApp.Core.ViewModels;

namespace RankingApp.Views;

/// <summary>Tournament list. The ⋮ menu and database backup moved to HomePage.</summary>
public partial class AllTournaments : BaseContentPage
{
    private readonly AllTournamentsViewModel _viewModel;

    public AllTournaments(AllTournamentsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
        _viewModel.SearchText = String.Empty;
    }
}
