using RankingApp.Core.Models;
using RankingApp.Core.ViewModels;

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
        // Search text is kept, like the other filters, when coming back to this page
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