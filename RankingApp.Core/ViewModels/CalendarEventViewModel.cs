using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Core.Models;
using RankingApp.Core.Services;
using RankingApp.Core.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RankingApp.Core.ViewModels
{
    /// <summary>
    /// One event of the calendar: name, date / time / coefficient and registered participants,
    /// numbered by the ranking of the current month (PlayerDB if it holds that month, otherwise downloaded; 0 if not found).
    /// </summary>
    public partial class CalendarEventViewModel(TournamentService tournamentService, IDatabaseService database, MonthRankingService rankingService) : BaseViewModel, IQueryAttributable
    {
        private readonly TournamentService _tournamentService = tournamentService;
        private readonly IDatabaseService _database = database;
        private readonly MonthRankingService _rankingService = rankingService;

        private int _eventId;
        private int _loadedEventId;
        private int _loadVersion;

        [ObservableProperty]
        private string title = "";

        [ObservableProperty]
        private string info = "";

        [ObservableProperty]
        private string participantsHeader = "";

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusText = "";

        public ObservableCollection<CalendarParticipantItem> Participants { get; } = [];

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (NavParams.TryGetInt(query, NavParams.EventId, out int id))
                _eventId = id;
        }

        public async Task LoadAsync()
        {
            if (_eventId <= 0)
                return;

            // Same event as before (e.g. coming back from another page) - keep what is shown
            if (_eventId == _loadedEventId && Participants.Count > 0)
                return;

            int version = ++_loadVersion;
            int eventId = _eventId;

            try
            {
                Participants.Clear();
                Title = "";
                Info = "";
                ParticipantsHeader = "";
                IsLoading = true;
                StatusText = "Loading event...";

                var result = await _tournamentService.GetEventResultsAsync(eventId);

                if (version != _loadVersion)
                    return;

                if (result?.CompetitionEvent == null)
                {
                    StatusText = "Could not load the event.";
                    return;
                }

                var ev = result.CompetitionEvent;

                Title = string.IsNullOrWhiteSpace(ev.Competition?.Name)
                    ? ev.Name
                    : $"{ev.Competition.Name}({ev.Name})";

                string dateTime = string.IsNullOrWhiteSpace(ev.StartTime) ? ev.StartDate : $"{ev.StartDate} {ev.StartTime}";
                Info = $"{dateTime} - {ev.RankingCoef}";

                // Future events: the current ranking (system month, or the previous one if not published yet)
                var now = DateTime.Now;
                var ranking = await _rankingService.GetAsync(now.Year, now.Month);

                var sorted = await EventResultsBuilder.BuildParticipantsAsync(_database, result.Participants, ranking);

                if (version != _loadVersion)
                    return;

                foreach (var item in sorted)
                    Participants.Add(item);

                _loadedEventId = eventId;
                ParticipantsHeader = $"Participants ({sorted.Count})";
                StatusText = sorted.Count == 0 ? "No participants registered yet." : "";
            }
            catch (Exception ex)
            {
                if (version == _loadVersion)
                    StatusText = $"Failed to load event: {ex.Message}";
            }
            finally
            {
                if (version == _loadVersion)
                    IsLoading = false;
            }
        }
    }
}
