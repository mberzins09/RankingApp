using RankingApp.Core.ViewModels;

namespace RankingApp.Views;

public partial class CalendarPage : BaseContentPage
{
    private readonly CalendarViewModel _viewModel;

    public CalendarPage(CalendarViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Initialize();
    }
}
