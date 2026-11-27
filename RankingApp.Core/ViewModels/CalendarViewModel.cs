using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Core.Interfaces;
using RankingApp.Core.Models;
using RankingApp.Core.Services;
using System.Collections.ObjectModel;

namespace RankingApp.Core.ViewModels
{
    /// <summary>
    /// Calendar: planned events of the selected year (competitions?year=..&amp;status=planned),
    /// grouped by competition. Tapping a competition shows its events.
    /// Participants of future events are ranked by the current month's ranking.
    /// </summary>
    public partial class CalendarViewModel(TournamentService tournamentService, MonthRankingService rankingService, INavigationService navigation,
        PlayerEventsService playerEvents) : BaseViewModel
    {
        private readonly TournamentService _tournamentService = tournamentService;
        private readonly PlayerEventsService _playerEvents = playerEvents;

        // Groups shown now - re-marked when the app player's / friends' events change
        private List<CompetitionGroupItem> _groups = [];
        private readonly MonthRankingService _rankingService = rankingService;
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

        /// <summary>CompetitionGroupItem rows, with CalendarEventItem rows of expanded competitions below them.</summary>
        public ObservableCollection<object> Rows { get; } = [];

        partial void OnSelectedYearChanged(int value)
        {
            if (value > 0)
                _ = LoadEventsAsync();
        }

        /// <summary>Called when the page appears. First time: system year; later the list is kept.</summary>
        public void Initialize()
        {
            var now = DateTime.Now;

            // Ranking of the current month for the event participants (from PlayerDB if it is that month)
            _rankingService.Prefetch(now.Year, now.Month);

            if (_initialized)
                return;

            _initialized = true;
            _playerEvents.Changed += OnPlayerEventsChanged;

            Years = new ObservableCollection<int>(Enumerable.Range(2020, now.Year + 2 - 2020).OrderByDescending(y => y));
            SelectedYear = now.Year;   // triggers loading
        }

        [RelayCommand]
        private async Task RefreshAsync() => await LoadEventsAsync();

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

            await _navigation.GoToAsync(NavParams.CalendarEventRoute, new Dictionary<string, object>
            {
                [NavParams.EventId] = item.EventId
            });
        }

        /// <summary>Background sync stored new event ids - update the gold / blue marks of the shown list.</summary>
        private void OnPlayerEventsChanged(object? sender, EventArgs e) =>
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    CompetitionListBuilder.ApplyMarks(_groups, await _playerEvents.GetMarksAsync());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Calendar marks not updated: {ex.Message}");
                }
            });

        private async Task LoadEventsAsync()
        {
            int version = ++_loadVersion;
            int year = SelectedYear;

            try
            {
                Rows.Clear();
                IsLoading = true;
                StatusText = "Loading events...";

                var competitions = await _tournamentService.GetCompetitionsAsync(year);

                // Year was changed again while loading - the newer request shows its result
                if (version != _loadVersion)
                    return;

                var events = competitions
                    .SelectMany(c => (c.Events ?? []).Select(e => new CalendarEventItem
                    {
                        EventId = e.Id,
                        CompetitionId = c.Id,
                        CompetitionName = c.Name,
                        EventName = e.Name,
                        StartDate = e.StartDate,
                        StartTime = e.StartTime ?? "",
                        RankingCoef = e.RankingCoef
                    }))
                    .ToList();

                var groups = CompetitionListBuilder.Group(events, newestFirst: false);

                foreach (var g in groups)
                    g.OpensDirectly = g.Events.Count == 1;

                CompetitionListBuilder.ApplyMarks(groups, await _playerEvents.GetMarksAsync());

                if (version != _loadVersion)
                    return;

                _groups = groups;
                CompetitionListBuilder.Fill(Rows, groups);

                StatusText = groups.Count == 0 ? $"No planned events found for {year}." : "";
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
    }
}
