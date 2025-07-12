using RankingApp.ViewModels;
using System.Collections.ObjectModel;
using RankingApp.Models;
using RankingApp.Data_Storage;
using RankingApp.Services;

namespace RankingApp.Views;

public partial class AllPlayerRanking : ContentPage
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
    }


    private async void ImageButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            await Application.Current.MainPage.DisplayAlert("Success", "Done", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Something went wrong:\n{ex.Message}", "OK");
        }
    }
}