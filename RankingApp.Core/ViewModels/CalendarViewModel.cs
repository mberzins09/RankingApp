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
    /// Calendar: planned events of the selected year (competitions?year=..&amp;status=planned).
    /// Every event of a competition is its own row.
    /// </summary>
    public partial class CalendarViewModel(TournamentService tournamentService, INavigationService navigation) : BaseViewModel
    {
        private readonly TournamentService _tournamentService = tournamentService;
        private readonly INavigationService _navigation = navigation;

        private int _loadVersion;
        private bool _initialized;

        [ObservableProperty]
        private ObservableCollection<int> years = [];

        [ObservableProperty]
        private int selectedYear;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusText = "";

        public ObservableCollection<CalendarEventItem> Events { get; } = [];

        partial void OnSelectedYearChanged(int value)
        {
            if (value > 0)
                _ = LoadEventsAsync();
        }

        /// <summary>Called when the page appears. First time: system year; later the list is kept.</summary>
        public void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            int current = DateTime.Now.Year;

            Years = new ObservableCollection<int>(Enumerable.Range(2020, current + 2 - 2020).OrderByDescending(y => y));
            SelectedYear = current;   // triggers loading
        }

        [RelayCommand]
        private async Task RefreshAsync() => await LoadEventsAsync();

        [RelayCommand]
        private async Task OpenEventAsync(CalendarEventItem? item)
        {
            if (item == null)
                return;

            await _navigation.GoToAsync(NavParams.CalendarEventRoute, new Dictionary<string, object>
            {
                [NavParams.EventId] = item.EventId
            });
        }

        private async Task LoadEventsAsync()
        {
            int version = ++_loadVersion;
            int year = SelectedYear;

            try
            {
                Events.Clear();
                IsLoading = true;
                StatusText = "Loading events...";

                var competitions = await _tournamentService.GetCompetitionsAsync(year);

                // Year was changed again while loading - the newer request shows its result
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
                    .OrderBy(i => ParseDate(i.StartDate) ?? DateTime.MaxValue)
                    .ThenBy(i => i.StartTime)
                    .ToList();

                foreach (var item in items)
                    Events.Add(item);

                StatusText = items.Count == 0 ? $"No planned events found for {year}." : "";
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

        private static DateTime? ParseDate(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }
}
