using CommunityToolkit.Maui.Storage;
using RankingApp.Core.ViewModels;
using SQLite;

namespace RankingApp.Views;

public partial class AllTournaments : BaseContentPage
{
    private readonly AllTournamentsViewModel _viewModel;

    private bool _menuOpen = false;

    private SQLiteConnection? sqliteConnection;

    public AllTournaments(AllTournamentsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        MoreMenuPanel.SizeChanged += MoreMenuPanel_SizeChanged;
    }

    private void MoreMenuPanel_SizeChanged(object? sender, EventArgs e)
    {
        if (!_menuOpen && MoreMenuPanel.Width > 0)
        {
            // start off-screen
            MoreMenuPanel.TranslationX = MoreMenuPanel.Width;
        }
    }

    private async void MoreButton_Clicked(object sender, EventArgs e)
    {
        if (!_menuOpen)
        {
            // show overlay and animate panel in
            MoreMenuOverlay.IsVisible = true;
            // ensure panel starts off-screen in case SizeChanged hasn't fired
            if (MoreMenuPanel.Width > 0)
                MoreMenuPanel.TranslationX = MoreMenuPanel.Width;

            await MoreMenuPanel.TranslateTo(0, 0, 250u, Easing.CubicOut);
            _menuOpen = true;
        }
        else
        {
            await CloseMenuAsync();
        }
    }

    async Task CloseMenuAsync()
    {
        if (!_menuOpen) return;
        await MoreMenuPanel.TranslateTo(MoreMenuPanel.Width, 0, 200, Easing.CubicIn);
        MoreMenuOverlay.IsVisible = false;
        _menuOpen = false;
    }

    private async void MoreOverlay_Tapped(object sender, EventArgs e)
    {
        await CloseMenuAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CheckRankingUpdateAsync(DateTime.Now);
        await _viewModel.LoadDataAsync();
        _viewModel.SearchText = String.Empty;
    }

    private async void MenuAdd_Clicked(object sender, EventArgs e)
    {
        await CloseMenuAsync();
        await _viewModel.CreateNewTournamentSave();
        await Shell.Current.GoToAsync(nameof(TournamentView));
    }

    private async void MenuAllGames_Clicked(object sender, EventArgs e)
    {
        await CloseMenuAsync();
        await Shell.Current.GoToAsync(nameof(AllGames));
    }

    private async void MenuRankings_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AllPlayerRanking));
        await CloseMenuAsync();
    }

    private async void MenuImport_Clicked(object sender, EventArgs e)
    {
        await CloseMenuAsync();
        await ImportDatabaseAsync();
        await _viewModel.LoadDataAsync();
    }

    private async void MenuExport_Clicked(object sender, EventArgs e)
    {
        await CloseMenuAsync();
        await ExportDatabaseAsync();
    }
    private async Task ExportDatabaseAsync()
    {
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "AllP.db3");
        string tempPath = Path.Combine(FileSystem.CacheDirectory, "temp.db3");

        if (!File.Exists(dbPath))
        {
            await DisplayAlert("Error", "Database not found!", "OK");
            return;
        }

        sqliteConnection?.Close();
        sqliteConnection?.Dispose();

        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Error", "Storage permission is required.", "OK");
                return;
            }
        }

        try
        {
            File.Copy(dbPath, tempPath, true);
            var fileInfo = new FileInfo(tempPath);
            if (fileInfo.Length == 0)
            {
                await DisplayAlert("Error", "Export failed: temp file is empty.", "OK");
                return;
            }
            await using var stream = File.OpenRead(tempPath);
            if (!stream.CanRead)
            {
                await DisplayAlert("Error", "Export failed: stream is not readable.", "OK");
                return;
            }

            var result = await FileSaver.Default.SaveAsync("backup.db3", stream, CancellationToken.None);

            if (result.IsSuccessful)
            {
                await DisplayAlert("Success", $"Exported to {result.FilePath}", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Export cancelled or failed", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Export failed: {ex.Message}", "OK");
        }
    }

    private async Task ImportDatabaseAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select database backup",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "application/octet-stream", "application/x-sqlite3" } },
                { DevicePlatform.iOS, new[] { "public.database", "public.data" } },
                { DevicePlatform.WinUI, new[] { ".db3", ".sqlite", ".db" } },
                { DevicePlatform.MacCatalyst, new[] { "public.database", "public.data" } }
            })
            });

            if (result == null)
                return; // User cancelled

            string destPath = Path.Combine(FileSystem.AppDataDirectory, "AllP.db3");

            using var sourceStream = await result.OpenReadAsync();
            using var destinationStream = File.Create(destPath);
            await sourceStream.CopyToAsync(destinationStream);

            await DisplayAlert("Success", "Database restored successfully!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Import failed: {ex.Message}", "OK");
        }
    }
}