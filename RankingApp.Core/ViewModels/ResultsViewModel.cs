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
    /// Results: events of the selected year + month that have started (system date), grouped by competition.
    /// Events of a year come from competitions?year=.. (singles, doubles) plus competition-events?type=teams (teams events,
    /// seasons split by playing date). Every loaded year is kept in memory, so switching the pickers needs no new API call.
    /// The ranking of the selected month is preloaded for the event results page.
    /// </summary>
    public partial class ResultsViewModel(TournamentService tournamentService, MonthRankingService rankingService, INavigationService navigation,
        PlayerEventsService playerEvents) : BaseViewModel
    {
        private readonly PlayerEventsService _playerEvents = playerEvents;

        private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

        private readonly TournamentService _tournamentService = tournamentService;
        private readonly MonthRankingService _rankingService = rankingService;
        private readonly INavigationService _navigation = navigation;

        // Groups shown for a year + month (built from the year's events)
        private readonly Dictionary<(int Year, int Month), (DateTime LoadedAt, List<CompetitionGroupItem> Groups)> _cache = [];

        // All events of a year (both API lists together)
        private readonly Dictionary<int, (DateTime LoadedAt, List<CalendarEventItem> Events)> _yearCache = [];

        // Teams events of all years (one call for every year) - not cached when the call failed / was empty
        private (DateTime LoadedAt, List<APITeamsEventListItem> Events)? _teamsCache;

        private int _loadVersion;
        private bool _initialized;
        private bool _suppressLoad;

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

        /// <summary>CompetitionGroupItem rows, with CalendarEventItem rows of expanded competitions below them.</summary>
        public ObservableCollection<object> Rows { get; } = [];

        partial void OnSelectedYearChanged(int value) => OnPickerChanged();

        partial void OnSelectedMonthChanged(MonthOption? value) => OnPickerChanged();

        private void OnPickerChanged()
        {
            if (_suppressLoad || SelectedYear <= 0 || SelectedMonth == null)
                return;

            _ = LoadAsync();
        }

        /// <summary>First time: system year and month. Later: reloads only if the shown month is old.</summary>
        public void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;
                _playerEvents.Changed += OnPlayerEventsChanged;

                var now = DateTime.Now;
                var names = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;

                var list = new List<MonthOption> { new() { Number = 0, Name = "All months" } };

                for (int m = 1; m <= 12; m++)
                    list.Add(new MonthOption { Number = m, Name = Capitalize(names[m - 1]) });

                _suppressLoad = true;
                Months = new ObservableCollection<MonthOption>(list);
                SelectedMonth = list.First(m => m.Number == now.Month);
                Years = new ObservableCollection<int>(Enumerable.Range(2020, Math.Max(1, now.Year - 2020 + 1)).OrderByDescending(y => y));
                SelectedYear = now.Year;
                _suppressLoad = false;

                _ = LoadAsync();
                return;
            }

            if (_cache.TryGetValue(CurrentKey, out var cached) && DateTime.UtcNow - cached.LoadedAt > CacheLifetime)
                _ = LoadAsync(forceReload: true);
        }

        private (int Year, int Month) CurrentKey => (SelectedYear, SelectedMonth?.Number ?? 0);

        [RelayCommand]
        private async Task RefreshAsync() => await LoadAsync(forceReload: true);

        [RelayCommand]
        private async Task ToggleCompetitionAsync(CompetitionGroupItem? group)
        {
            if (group == null)
                return;

            // Only one event - no dropdown, open it straight away
            if (group.OpensDirectly)
            {
                await OpenEventAsync(group.Events[0]);
                return;
            }

            CompetitionListBuilder.Toggle(Rows, group);
        }

        [RelayCommand]
        private async Task OpenEventAsync(CalendarEventItem? item)
        {
            if (item == null)
                return;

            // The results page uses the ranking of the event's month - start loading it now
            if (CompetitionListBuilder.ParseDate(item.StartDate) is { } date)
                _rankingService.Prefetch(date.Year, date.Month);

            await _navigation.GoToAsync(NavParams.EventResultsRoute, new Dictionary<string, object>
            {
                [NavParams.EventId] = item.EventId,
                [NavParams.PlayingDate] = item.PlayingDate
            });
        }

        private async Task LoadAsync(bool forceReload = false)
        {
            int version = ++_loadVersion;
            var key = CurrentKey;

            if (key.Month > 0)
                _rankingService.Prefetch(key.Year, key.Month);

            // Already loaded this year + month - no new API call
            if (!forceReload && _cache.TryGetValue(key, out var cached))
            {
                IsLoading = false;
                Show(key, cached.Groups);
                return;
            }

            try
            {
                Rows.Clear();
                IsLoading = true;
                StatusText = "Loading events...";

                var (loadedAt, yearEvents) = await GetYearEventsAsync(key.Year, forceReload);

                if (version != _loadVersion)
                    return;

                var today = DateTime.Today;

                var events = yearEvents
                    .Where(e =>
                    {
                        // Results only: events that have started (future ones are in the Calendar)
                        var date = CompetitionListBuilder.ParseDate(e.StartDate);
                        return date != null && date.Value <= today &&
                               (key.Month == 0 || (date.Value.Year == key.Year && date.Value.Month == key.Month));
                    })
                    .ToList();

                var groups = CompetitionListBuilder.Group(events, newestFirst: true);

                foreach (var g in groups)
                    g.OpensDirectly = g.Events.Count == 1;

                CompetitionListBuilder.ApplyMarks(groups, await _playerEvents.GetMarksAsync());

                if (version != _loadVersion)
                    return;

                _cache[key] = (loadedAt, groups);
                Show(key, groups);
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

        /// <summary>
        /// All events of the year: competitions?year=.. (throws on failure, so nothing is cached) + teams events.
        /// A failed teams call only leaves the teams events out.
        /// </summary>
        private async Task<(DateTime LoadedAt, List<CalendarEventItem> Events)> GetYearEventsAsync(int year, bool forceReload)
        {
            if (!forceReload && _yearCache.TryGetValue(year, out var cached) && DateTime.UtcNow - cached.LoadedAt <= CacheLifetime)
                return cached;

            var competitionsTask = _tournamentService.GetAllCompetitionsAsync(year);
            var teamsTask = GetTeamsEventsAsync(forceReload);

            await Task.WhenAll(competitionsTask, teamsTask);

            var loaded = (DateTime.UtcNow, CompetitionListBuilder.ResultsEvents(year, competitionsTask.Result, teamsTask.Result));

            // Groups of this year's months were built from the old list
            foreach (var key in _cache.Keys.Where(k => k.Year == year).ToList())
                _cache.Remove(key);

            _yearCache[year] = loaded;
            return loaded;
        }

        private async Task<List<APITeamsEventListItem>> GetTeamsEventsAsync(bool forceReload)
        {
            if (!forceReload && _teamsCache is { } cached && DateTime.UtcNow - cached.LoadedAt <= CacheLifetime)
                return cached.Events;

            var events = await _tournamentService.GetTeamsEventsAsync();

            if (events.Count > 0)
                _teamsCache = (DateTime.UtcNow, events);

            return events.Count > 0 ? events : _teamsCache?.Events ?? [];
        }

        /// <summary>Background sync stored new event ids - update the gold / blue marks of every loaded month.</summary>
        private void OnPlayerEventsChanged(object? sender, EventArgs e) =>
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var marks = await _playerEvents.GetMarksAsync();

                    foreach (var (_, groups) in _cache.Values)
                        CompetitionListBuilder.ApplyMarks(groups, marks);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Results marks not updated: {ex.Message}");
                }
            });

        private void Show((int Year, int Month) key, List<CompetitionGroupItem> groups)
        {
            CompetitionListBuilder.Fill(Rows, groups);

            StatusText = groups.Count > 0
                ? ""
                : key.Month == 0
                    ? $"No events found for {key.Year}."
                    : $"No events in {SelectedMonth?.Name} {key.Year}.";
        }

        private static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], CultureInfo.CurrentCulture) + s[1..];
    }
}
