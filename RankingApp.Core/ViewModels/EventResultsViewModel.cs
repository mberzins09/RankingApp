using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Core.Models;
using RankingApp.Core.Services;
using RankingApp.Core.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RankingApp.Core.ViewModels
{
    /// <summary>
    /// Results of one event. The response is loaded once and turned into ready models (participants, group
    /// tables, brackets); the picker only switches between them, so switching needs no work and no API call.
    /// </summary>
    public partial class EventResultsViewModel(TournamentService tournamentService, IDatabaseService database) : BaseViewModel, IQueryAttributable
    {
        private readonly TournamentService _tournamentService = tournamentService;
        private readonly IDatabaseService _database = database;

        // Everything built for an event - kept for the whole app session
        private readonly Dictionary<int, EventResultsData> _cache = [];

        private int _eventId;
        private int _loadVersion;

        private sealed class EventResultsData
        {
            public string Title { get; init; } = "";
            public string Info { get; init; } = "";
            public List<CalendarParticipantItem> Participants { get; init; } = [];
            public List<ResultViewOption> Options { get; init; } = [];
            public List<ResultViewOption> StageOptions { get; init; } = [];
            public Dictionary<string, GroupTableModel> Groups { get; init; } = [];
            public Dictionary<string, BracketModel> Brackets { get; init; } = [];
        }

        private EventResultsData? _current;

        [ObservableProperty]
        private string title = "";

        [ObservableProperty]
        private string info = "";

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusText = "";

        [ObservableProperty]
        private ObservableCollection<ResultViewOption> options = [];

        [ObservableProperty]
        private ResultViewOption? selectedOption;

        /// <summary>Off (default): one picker entry per group / bracket. On: one entry per stage, everything under each other.</summary>
        [ObservableProperty]
        private bool sortByStage;

        [ObservableProperty]
        private bool isParticipantsSelected;

        [ObservableProperty]
        private bool isDrawingSelected;

        /// <summary>Id of the event whose data is shown (the page uses it for its own view cache).</summary>
        public int LoadedEventId { get; private set; }

        /// <summary>Increased every time new data is applied (load or refresh) - the page drops its drawn views then.</summary>
        public int DataVersion { get; private set; }

        public ObservableCollection<CalendarParticipantItem> Participants { get; } = [];

        partial void OnSelectedOptionChanged(ResultViewOption? value)
        {
            IsParticipantsSelected = value?.Kind == ResultViewKind.Participants;
            IsDrawingSelected = value != null && value.Kind != ResultViewKind.Participants;
        }

        partial void OnSortByStageChanged(bool value)
        {
            if (_current == null)
                return;

            var previous = SelectedOption;
            var list = OptionsFor(_current);

            Options = new ObservableCollection<ResultViewOption>(list);
            SelectedOption = PickOption(list, previous);
        }

        private List<ResultViewOption> OptionsFor(EventResultsData data) => SortByStage ? data.StageOptions : data.Options;

        private ResultViewOption PickOption(List<ResultViewOption> list, ResultViewOption? previous)
        {
            if (previous == null)
                return list[0];

            var same = list.FirstOrDefault(o => o.Key == previous.Key);
            if (same != null)
                return same;

            // Switching between the two modes: a group / bracket -> the stage that holds it, a stage -> its first group / bracket
            if (previous.Kind is ResultViewKind.Group or ResultViewKind.Bracket)
                return list.FirstOrDefault(o => o.Kind == ResultViewKind.Stage && o.ItemKeys.Contains(previous.Key)) ?? list[0];

            if (previous.Kind == ResultViewKind.Stage && previous.ItemKeys.Count > 0)
                return list.FirstOrDefault(o => o.Key == previous.ItemKeys[0]) ?? list[0];

            return list[0];
        }

        public GroupTableModel? GetGroupTable(string key) =>
            _current != null && _current.Groups.TryGetValue(key, out var g) ? g : null;

        public BracketModel? GetBracket(string key) =>
            _current != null && _current.Brackets.TryGetValue(key, out var b) ? b : null;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (NavParams.TryGetInt(query, NavParams.EventId, out int id))
                _eventId = id;
        }

        public async Task LoadAsync()
        {
            if (_eventId <= 0)
                return;

            // Same event as before (coming back to the page) - keep what is shown, including the picker choice
            if (_eventId == LoadedEventId && Options.Count > 0)
                return;

            int version = ++_loadVersion;
            int eventId = _eventId;

            try
            {
                if (!_cache.TryGetValue(eventId, out var data))
                {
                    Clear();
                    IsLoading = true;
                    StatusText = "Loading results...";

                    var result = await _tournamentService.GetEventResultsAsync(eventId);

                    if (version != _loadVersion)
                        return;

                    if (result?.CompetitionEvent == null)
                    {
                        StatusText = "Could not load the event.";
                        return;
                    }

                    data = await BuildAsync(result);

                    if (version != _loadVersion)
                        return;

                    _cache[eventId] = data;
                }

                Apply(eventId, data);
            }
            catch (Exception ex)
            {
                if (version == _loadVersion)
                    StatusText = $"Failed to load results: {ex.Message}";
            }
            finally
            {
                if (version == _loadVersion)
                    IsLoading = false;
            }
        }

        /// <summary>Calls the API again for this event (new games of an ongoing tournament) and keeps the picker choice.</summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (_eventId <= 0 || IsLoading)
                return;

            int version = ++_loadVersion;
            int eventId = _eventId;
            var keepOption = SelectedOption;

            try
            {
                IsLoading = true;
                StatusText = "Refreshing...";

                var result = await _tournamentService.GetEventResultsAsync(eventId);

                if (version != _loadVersion)
                    return;

                if (result?.CompetitionEvent == null)
                {
                    StatusText = "Could not refresh - showing the previous results.";
                    return;
                }

                var data = await BuildAsync(result);

                if (version != _loadVersion)
                    return;

                _cache[eventId] = data;
                Apply(eventId, data, keepOption);
            }
            catch (Exception ex)
            {
                if (version == _loadVersion)
                    StatusText = $"Refresh failed: {ex.Message}";
            }
            finally
            {
                if (version == _loadVersion)
                    IsLoading = false;
            }
        }

        private async Task<EventResultsData> BuildAsync(APIEventResultsResponse result)
        {
            var ev = result.CompetitionEvent!;

            string dateTime = string.IsNullOrWhiteSpace(ev.StartTime) ? ev.StartDate : $"{ev.StartDate} {ev.StartTime}";

            var options = new List<ResultViewOption>
            {
                new() { Key = "participants", Title = "Participants", Kind = ResultViewKind.Participants }
            };

            var groups = new Dictionary<string, GroupTableModel>();
            var brackets = new Dictionary<string, BracketModel>();

            var nets = (result.Nets ?? []).OrderBy(n => n.Order).ToList();
            bool severalGroupNets = nets.Count(n => n.Groups is { Count: > 0 }) > 1;

            foreach (var net in nets)
            {
                foreach (var group in (net.Groups ?? []).OrderBy(g => g.GroupNumber))
                {
                    string key = EventResultsBuilder.GroupKey(net, group);
                    string groupTitle = EventResultsBuilder.GroupTitle(net, group, severalGroupNets);

                    groups[key] = EventResultsBuilder.BuildGroupTable(groupTitle, group);
                    options.Add(new ResultViewOption { Key = key, Title = groupTitle, Kind = ResultViewKind.Group });
                }

                foreach (var tree in net.EliminationTrees ?? [])
                {
                    string key = EventResultsBuilder.TreeKey(net, tree);
                    string treeTitle = EventResultsBuilder.TreeTitle(tree);

                    brackets[key] = EventResultsBuilder.BuildBracket(treeTitle, tree);
                    options.Add(new ResultViewOption { Key = key, Title = treeTitle, Kind = ResultViewKind.Bracket });
                }
            }

            // Stage mode: Participants + one entry per stage (= net) with all its groups / brackets under each other
            var stageOptions = new List<ResultViewOption> { options[0] };
            int stageNumber = 0;

            foreach (var net in nets)
            {
                var keys = new List<string>();

                foreach (var group in (net.Groups ?? []).OrderBy(g => g.GroupNumber))
                    keys.Add(EventResultsBuilder.GroupKey(net, group));

                foreach (var tree in net.EliminationTrees ?? [])
                    keys.Add(EventResultsBuilder.TreeKey(net, tree));

                if (keys.Count == 0)
                    continue;

                stageNumber++;
                string stageTitle = EventResultsBuilder.StageTitle(net, stageNumber, (net.Groups?.Count ?? 0) > 0, (net.EliminationTrees?.Count ?? 0) > 0);

                stageOptions.Add(new ResultViewOption
                {
                    Key = $"stage:{net.Id}",
                    Title = stageTitle,
                    Heading = stageTitle,
                    Kind = ResultViewKind.Stage,
                    ItemKeys = keys
                });
            }

            return new EventResultsData
            {
                StageOptions = stageOptions,
                Title = string.IsNullOrWhiteSpace(ev.Competition?.Name) ? ev.Name : $"{ev.Competition.Name}({ev.Name})",
                Info = $"{dateTime} - {ev.RankingCoef}",
                Participants = await EventResultsBuilder.BuildParticipantsAsync(_database, result.Participants),
                Options = options,
                Groups = groups,
                Brackets = brackets
            };
        }

        private void Apply(int eventId, EventResultsData data, ResultViewOption? keepOption = null)
        {
            _current = data;
            LoadedEventId = eventId;
            DataVersion++;

            Title = data.Title;
            Info = data.Info;

            Participants.Clear();

            foreach (var p in data.Participants)
                Participants.Add(p);

            var list = OptionsFor(data);
            Options = new ObservableCollection<ResultViewOption>(list);
            SelectedOption = PickOption(list, keepOption);

            StatusText = "";
        }

        private void Clear()
        {
            _current = null;
            LoadedEventId = 0;
            Title = "";
            Info = "";
            Participants.Clear();
            Options = [];
            SelectedOption = null;
        }
    }
}
