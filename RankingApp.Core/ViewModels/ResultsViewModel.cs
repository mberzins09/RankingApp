using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Core.Interfaces;
using RankingApp.Core.Models;
using RankingApp.Core.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RankingApp.Core.ViewModels
{
    /// <summary>
    /// Results: finished (and running) events of the selected year. status=ongoing returns both past and
    /// ongoing competitions. A whole year is loaded once and cached - the month picker only filters the cache.
    /// </summary>
    public partial class ResultsViewModel(TournamentService tournamentService, INavigationService navigation) : BaseViewModel
    {
        private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

        private readonly TournamentService _tournamentService = tournamentService;
        private readonly INavigationService _navigation = navigation;

        private readonly Dictionary<int, (DateTime LoadedAt, List<CalendarEventItem> Events)> _yearCache = [];

        private int _loadVersion;
        private bool _initialized;

        [ObservableProperty]
        private ObservableCollection<int> years = [];

        [ObservableProperty]
        private int selectedYear;

        [ObservableProperty]
        private ObservableCollection<MonthOption> months = [];

        [ObservableProperty]
        private MonthOption? selectedMonth;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusText = "";

        public ObservableCollection<CalendarEventItem> Events { get; } = [];

        partial void OnSelectedYearChanged(int value)
        {
            if (value > 0)
                _ = LoadYearAsync();
        }

        partial void OnSelectedMonthChanged(MonthOption? value) => ApplyMonthFilter();

        /// <summary>First time: system year and month. Later: only reloads a year whose cache is old.</summary>
        public void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;

                var now = DateTime.Now;

                var names = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;

                var list = new List<MonthOption> { new() { Number = 0, Name = "All months" } };

                for (int m = 1; m <= 12; m++)
                    list.Add(new MonthOption { Number = m, Name = Capitalize(names[m - 1]) });

                Months = new ObservableCollection<MonthOption>(list);
                SelectedMonth = list.First(m => m.Number == now.Month);

                Years = new ObservableCollection<int>(Enumerable.Range(2020, Math.Max(1, now.Year - 2020 + 1)).OrderByDescending(y => y));
                SelectedYear = now.Year;   // triggers loading
                return;
            }

            if (_yearCache.TryGetValue(SelectedYear, out var cached) && DateTime.UtcNow - cached.LoadedAt > CacheLifetime)
                _ = LoadYearAsync(forceReload: true);
        }

        [RelayCommand]
        private async Task OpenEventAsync(CalendarEventItem? item)
        {
            if (item == null)
                return;

            await _navigation.GoToAsync(NavParams.EventResultsRoute, new Dictionary<string, object>
            {
                [NavParams.EventId] = item.EventId
            });
        }

        private async Task LoadYearAsync(bool forceReload = false)
        {
            int version = ++_loadVersion;
            int year = SelectedYear;

            // Already loaded this year - only filter, no new API call
            if (!forceReload && _yearCache.ContainsKey(year))
            {
                IsLoading = false;
                ApplyMonthFilter();
                return;
            }

            try
            {
                Events.Clear();
                IsLoading = true;
                StatusText = "Loading events...";

                // "ongoing" gives past and ongoing competitions together
                var competitions = await _tournamentService.GetCompetitionsAsync(year, "ongoing");

                if (version != _loadVersion)
                    return;

                var items = competitions
                    .SelectMany(c => (c.Events ?? []).Select(e => new CalendarEventItem
                    {
                        EventId = e.Id,
                        CompetitionName = c.Name,
                        EventName = e.Name,
                        StartDate = e.StartDate,
                        StartTime = e.StartTime ?? "",
                        RankingCoef = e.RankingCoef
                    }))
                    .OrderByDescending(i => ParseDate(i.StartDate) ?? DateTime.MinValue)
                    .ThenBy(i => i.StartTime)
                    .ToList();

                // A failed / empty response is not cached, so it is tried again next time
                if (items.Count > 0)
                    _yearCache[year] = (DateTime.UtcNow, items);

                ApplyMonthFilter();
            }
            catch (Exception ex)
            {
                if (version == _loadVersion)
                    StatusText = $"Failed to load events: {ex.Message}";
            }
            finally
            {
                if (version == _loadVersion)
                    IsLoading = false;
            }
        }

        /// <summary>Shows the cached events of the selected year that start in the selected month.</summary>
        private void ApplyMonthFilter()
        {
            if (SelectedYear <= 0 || !_yearCache.TryGetValue(SelectedYear, out var cached))
                return;

            int month = SelectedMonth?.Number ?? 0;

            var filtered = month == 0
                ? cached.Events
                : cached.Events.Where(e => ParseDate(e.StartDate)?.Month == month).ToList();

            Events.Clear();

            foreach (var e in filtered)
                Events.Add(e);

            StatusText = filtered.Count == 0
                ? (month == 0 ? $"No events found for {SelectedYear}." : $"No events in {SelectedMonth?.Name} {SelectedYear}.")
                : "";
        }

        private static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], CultureInfo.CurrentCulture) + s[1..];

        private static DateTime? ParseDate(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }
}
