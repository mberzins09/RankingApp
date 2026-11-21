using RankingApp.Core.ViewModels;

namespace RankingApp.Views;

public partial class CalendarEventPage : BaseContentPage
{
    private readonly CalendarEventViewModel _viewModel;

    public CalendarEventPage(CalendarEventViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
