using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Models;
using RankingApp.Services;
using RankingApp.Views;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RankingApp.ViewModels
{
    public partial class PlayerViewModel(PlayerService playerService) : BaseViewModel
    {
        private readonly PlayerService _playerService = playerService;
        private List<PlayerDB>? _allPlayers = new();
        private List<PlayerDB>? _filteredPlayers = new();
        private AppData? _cachedAppData;
        private System.Timers.Timer? _searchDebounceTimer;

        [ObservableProperty]
        private DateTime minDate = new(2014, 1, 1);

        [ObservableProperty]
        private DateTime maxDate = DateTime.Today.AddDays(10);

        [ObservableProperty]
        private ObservableCollection<PlayerDB>? players;

        [ObservableProperty]
        private string selectedFilter = "All";

        [ObservableProperty]
        private DateTime selectedDate = DateTime.UtcNow;

        [ObservableProperty]
        private string? appDataLabel;

        [ObservableProperty]
        private string? searchText;

        public List<string> FilterOptions { get; } = ["Men", "Women", "All", "Inactive"];

        [RelayCommand]
        public async Task SetAsDefaultPlayerAsync(PlayerDB player)
        {
            if (player == null)
                return;

            var appData = await _playerService.GetAppDataAsync();
            appData.AppUserPlayerId = player.Id;
            await _playerService.SaveAppDataAsync(appData);
        }

        public async Task UpdateAllPlayersAsync()
        {
            var popup = new ProcessingPopup { Message = "Starting process..." };
            _ = Application.Current.MainPage.ShowPopupAsync(popup);

            try
            {
                await _playerService.DeleteAllPlayersInDatabse();
                await _playerService.FillDatabaseWithIdChangeTransitionAsync(status =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        popup.Message = status;
                    });
                });

                await Task.Delay(1500);
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() => popup.Message = $"Error: {ex.Message}");
                await Task.Delay(3000);
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await popup.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error closing popup: {ex.Message}");
                    }
                });
            }
        }

        partial void OnSelectedFilterChanged(string value)
        {
            FilterPlayers();
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            if (_cachedAppData == null)
            {
                _ = LoadPlayersFromApiAsync(value);
                return;
            }
            
            if (_cachedAppData.CurrentYear == value.Year &&
                _cachedAppData.CurrentMonth == value.Month)
            {
                return;
            }

            _ = LoadPlayersFromApiAsync(value);
        }

        partial void OnSearchTextChanged(string? value)
        {
            _searchDebounceTimer?.Stop();
            _searchDebounceTimer = new System.Timers.Timer(750); // 750ms debounce
            _searchDebounceTimer.Elapsed += (s, e) =>
            {
                _searchDebounceTimer?.Stop();
                MainThread.BeginInvokeOnMainThread(ApplySearch);
            };
            _searchDebounceTimer.Start();
        }

        public async Task LoadDataAsync()
        {
            _allPlayers = await _playerService.GetPlayersFromDbAsync();
            _cachedAppData = await _playerService.GetAppDataAsync();
            FilterPlayers();
            await UpdateAppDataLabel();
        }

        public async Task LoadPlayersFromApiAsync(DateTime? date = null)
        {
            var popup = new ProcessingPopup { Message = "Loading players..." };
            _ = Application.Current.MainPage.ShowPopupAsync(popup);

            try
            {
                _allPlayers = await _playerService.LoadPlayersFromApiOrDbAsync(date, status => MainThread.BeginInvokeOnMainThread(() => popup.Message = status));
                _cachedAppData = await _playerService.GetAppDataAsync();
                FilterPlayers();
                await UpdateAppDataLabel();
                MainThread.BeginInvokeOnMainThread(() => popup.Message = "✅ Players loaded successfully!");
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    popup.Message = $"❌ Error: {ex.Message}";
                });
            }
            finally
            {
                await Task.Delay(1000); // short pause to show final message
                await MainThread.InvokeOnMainThreadAsync(async () => await popup.CloseAsync());
            }
        }

        private void FilterPlayers()
        {
            IEnumerable<PlayerDB> filtered = _allPlayers;
            switch (SelectedFilter)
            {
                case "Men":
                    filtered = _allPlayers.Where(x => x.Gender == "male" && x.Place < 6000).OrderBy(x => x.Place);
                    break;
                case "Women":
                    filtered = _allPlayers.Where(x => x.Gender == "female" && x.Place < 6000).OrderBy(x => x.Place);
                    break;
                case "Inactive":
                    filtered = _allPlayers.Where(x => x.OverallPlace != 0 && x.OverallPlace > 5999)
                                          .OrderByDescending(x => x.PointsWithBonus);
                    break;
                case "All":
                default:
                    filtered = _allPlayers.Where(x => x.OverallPlace != 0 && x.OverallPlace < 6000)
                                          .OrderBy(x => x.OverallPlace);
                    break;
            }

            _filteredPlayers = filtered.ToList();
            ApplySearch();
        }

        private void ApplySearch()
        {
            if (_filteredPlayers == null)
                return;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Players = new ObservableCollection<PlayerDB>(_filteredPlayers);
                return;
            }

            var input = SearchText.Trim();
            IEnumerable<PlayerDB> result = _filteredPlayers;

            int GetPlace(PlayerDB p) => SelectedFilter == "All" ? p.OverallPlace : p.Place;

            var rangePattern = @"^\s*(\d+)\s*[-.]{1,2}\s*(\d+)\s*$";
            var greaterThanPattern = @"^>\s*(\d+)$";
            var greaterOrEqualPattern = @"^>=\s*(\d+)$";
            var lessThanPattern = @"^<\s*(\d+)$";
            var lessOrEqualPattern = @"^<=\s*(\d+)$";

            switch (input)
            {
                case var s when Regex.IsMatch(s, rangePattern):
                    var match = Regex.Match(s, rangePattern);
                    int start = int.Parse(match.Groups[1].Value);
                    int end = int.Parse(match.Groups[2].Value);
                    result = result.Where(p => {
                        var val = GetPlace(p);
                        return val >= start && val <= end;
                    });
                    break;

                case var s when Regex.IsMatch(s, greaterOrEqualPattern):
                    int val = int.Parse(Regex.Match(s, greaterOrEqualPattern).Groups[1].Value);
                    result = result.Where(p => GetPlace(p) >= val);
                    break;

                case var s when Regex.IsMatch(s, greaterThanPattern):
                    val = int.Parse(Regex.Match(s, greaterThanPattern).Groups[1].Value);
                    result = result.Where(p => GetPlace(p) > val);
                    break;

                case var s when Regex.IsMatch(s, lessOrEqualPattern):
                    val = int.Parse(Regex.Match(s, lessOrEqualPattern).Groups[1].Value);
                    result = result.Where(p => GetPlace(p) <= val);
                    break;

                case var s when Regex.IsMatch(s, lessThanPattern):
                    val = int.Parse(Regex.Match(s, lessThanPattern).Groups[1].Value);
                    result = result.Where(p => GetPlace(p) < val);
                    break;

                default:
                    result = result.Where(p =>
                    (!string.IsNullOrWhiteSpace(p.Name) && p.Name.StartsWith(input, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(p.Surname) && p.Surname.StartsWith(input, StringComparison.OrdinalIgnoreCase)) ||
                    p.Place.ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase)
                );
                    break;
            }

            Players = new ObservableCollection<PlayerDB>(result);
        }

        private async Task UpdateAppDataLabel()
        {
            var appData = await _playerService.GetAppDataAsync();
            if (appData.CurrentYear > 0 && appData.CurrentMonth > 0)
            {
                var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(appData.CurrentMonth);
                AppDataLabel = $"{appData.CurrentYear} {monthName}";
            }
            else
            {
                AppDataLabel = string.Empty;
            }
        }
    }
}
