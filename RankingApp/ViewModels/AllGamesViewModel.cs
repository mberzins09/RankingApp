using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;

namespace RankingApp.ViewModels
{
    public partial class AllGamesViewModel(DatabaseService database) : BaseViewModel
    {
        private readonly DatabaseService _database = database;

        private List<Game> _allGames = [];

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private ObservableCollection<Game>? games;

        partial void OnSearchTextChanged(string? value)
        {
            FilterGames(value ?? string.Empty);
        }

        public async Task LoadDataAsync()
        {
            var games = await _database.GetGamesAsync();
            _allGames = games.OrderByDescending(x => x.TournamentDate).ToList();
            Games = new ObservableCollection<Game>(_allGames);
        }

        public void FilterGames(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                Games = new ObservableCollection<Game>(_allGames);
                return;
            }

            var searchedGames = _allGames.Where(x => (!string.IsNullOrWhiteSpace(x.Name) &&
                                                x.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                                (!string.IsNullOrWhiteSpace(x.Surname) &&
                                                x.Surname.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))).ToList();

            Games = new ObservableCollection<Game>(searchedGames);
        }
    }
}
