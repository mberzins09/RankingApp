namespace RankingApp.Views;

public partial class AddTournament : ContentPage
{
	public AddTournament()
	{
		InitializeComponent();
	}

    private void BtnAddGame_OnClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync(nameof(AddGame));
    }

    private void BtnEditGame_OnClicked(object? sender, EventArgs e)
    {
        Shell.Current.GoToAsync(nameof(EditGame));
    }
}