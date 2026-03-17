using RankingApp.Models;
using RankingApp.ViewModels;

namespace RankingApp.Views;

public partial class AllPlayerRanking : BaseContentPage
{
    private readonly PlayerViewModel _viewModel;

    public AllPlayerRanking(PlayerViewModel viewModel)
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

    private void ListView_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        if (BindingContext is PlayerViewModel vm && e.Item is PlayerListItem item)
        {
            vm.PlayerSelectedCommand.Execute(item.Player);
        }

    ((ListView)sender).SelectedItem = null;
    }
}