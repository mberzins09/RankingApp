using CommunityToolkit.Maui.Storage;

namespace RankingApp.Views;

public partial class ImportExport : ContentPage
{
	public ImportExport()
	{
		InitializeComponent();
	}

    private async void OnExportClicked(object sender, EventArgs e)
    {
        await ExportDatabaseAsync();
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        await ImportDatabaseAsync();
    }

    private async Task ExportDatabaseAsync()
    {
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "AllP.db3");

        if (!File.Exists(dbPath))
        {
            await DisplayAlert("Error", "Database not found!", "OK");
            return;
        }

        try
        {
            // Open stream inside using
            await using var stream = File.OpenRead(dbPath);

            var result = await FileSaver.Default.SaveAsync(
                "backup.db3",       // suggested filename
                stream,             // stream to copy
                CancellationToken.None);

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

            string destPath = Path.Combine(FileSystem.AppDataDirectory, "mydb.db3");

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