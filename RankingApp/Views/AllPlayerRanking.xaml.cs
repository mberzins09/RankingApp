using RankingApp.Models;
using RankingApp.ViewModels;

namespace RankingApp.Views;

public partial class AllPlayerRanking : ContentPage
{
    private readonly PlayerViewModel _viewModel;

    private bool _menuOpen = false;

    public AllPlayerRanking(PlayerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        //MoreMenuPanel.SizeChanged += MoreMenuPanel_SizeChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
        _viewModel.SearchText = String.Empty;
    }

    //private void MoreMenuPanel_SizeChanged(object? sender, EventArgs e)
    //{
    //    if (!_menuOpen && MoreMenuPanel.Width > 0)
    //    {
    //        MoreMenuPanel.TranslationX = MoreMenuPanel.Width;
    //    }
    //}

    //private async void Dots_Clicked(object sender, EventArgs e)
    //{
    //    if (!_menuOpen)
    //    {
    //        MoreMenuOverlay.IsVisible = true;
    //        if (MoreMenuPanel.Width > 0)
    //            MoreMenuPanel.TranslationX = MoreMenuPanel.Width;

    //        await MoreMenuPanel.TranslateTo(0, 0, 250u, Easing.CubicOut);
    //        _menuOpen = true;
    //    }
    //    else
    //    {
    //        await CloseMenuAsync();
    //    }
    //}

    //async Task CloseMenuAsync()
    //{
    //    if (!_menuOpen) return;
    //    await MoreMenuPanel.TranslateTo(MoreMenuPanel.Width, 0, 200, Easing.CubicIn);
    //    MoreMenuOverlay.IsVisible = false;
    //    _menuOpen = false;
    //}

    //private async void MoreOverlay_Tapped(object sender, EventArgs e)
    //{
    //    await CloseMenuAsync();
    //}

    private void ListView_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        if (BindingContext is PlayerViewModel vm && e.Item is PlayerDB player)
        {
            vm.PlayerSelectedCommand.Execute(player);
        }

    ((ListView)sender).SelectedItem = null; // prevent stuck selection
    }
}