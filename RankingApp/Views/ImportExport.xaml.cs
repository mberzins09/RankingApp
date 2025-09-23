using CommunityToolkit.Maui.Storage;
using SQLite;
using System.Runtime.Versioning;
using Microsoft.Maui.ApplicationModel;

namespace RankingApp.Views;

public partial class ImportExport : ContentPage
{
    // Add this field to the ImportExport class to fix CS0103
    private SQLiteConnection? sqliteConnection;

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

    [SupportedOSPlatform("android")]
    [SupportedOSPlatform("ios14.0")]
    [SupportedOSPlatform("maccatalyst14.0")]
    [SupportedOSPlatform("windows")]
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

            var textStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Test"));
            var result = await FileSaver.Default.SaveAsync("test.txt", textStream, CancellationToken.None);

            //var result = await FileSaver.Default.SaveAsync("backup.db3", stream, CancellationToken.None);

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