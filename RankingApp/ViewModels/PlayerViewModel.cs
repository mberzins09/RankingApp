using System.Collections.ObjectModel;
using RankingApp.Models;
using RankingApp.Services;

namespace RankingApp.ViewModels
{
    public class PlayerViewModel(DatabaseService databaseService) : BaseViewModel
    {
        private readonly DatabaseService _databaseService = databaseService;

        public ObservableCollection<PlayerDB>? Mens { get; set; }
        public ObservableCollection<PlayerDB>? Womens { get; set; }
        public ObservableCollection<PlayerDB>? Players { get; set; }
        public ObservableCollection<PlayerDB>? InactivePlayers { get; set; }
        public async Task LoadPlayersAsyncFromDatabase()
        {
            var players = await _databaseService.GetPlayersAsync();
            Mens = new ObservableCollection<PlayerDB>(players
                                                        .Where(x => x.Gender == "male" & x.Place < 5000)
                                                        .OrderBy(x => x.Place)
                                                        .ToList());
            Womens = new ObservableCollection<PlayerDB>(players
                                                        .Where(x => x.Gender == "female" & x.Place < 5000)
                                                        .OrderBy(x => x.Place)
                                                        .ToList());
            Players = new ObservableCollection<PlayerDB>(players
                                                        .Where(x => x.OverallPlace != 0 & x.OverallPlace < 5000)
                                                        .OrderBy(x => x.OverallPlace)
                                                        .ToList());
            InactivePlayers = new ObservableCollection<PlayerDB>(players
                                                        .Where(x => x.OverallPlace != 0 & x.OverallPlace > 5000)
                                                        .OrderByDescending(x => x.PointsWithBonus)
                                                        .ToList());
            OnPropertyChanged();
        }

        public List<PlayerDB> SearchPlayers(List<PlayerDB> players, string filterText)
        {
            var searchedPlayers = players
                .Where(x => (!string.IsNullOrWhiteSpace(x.Name) &&
                             x.Name.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Surname) &&
                             x.Surname.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Place.ToString()) &&
                            x.Place.ToString().StartsWith(filterText, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return searchedPlayers;
        }

        public List<PlayerDB> SearchPlayersAllRanking(List<PlayerDB> players, string filterText)
        {
            var searchedPlayers = players
                .Where(x => (!string.IsNullOrWhiteSpace(x.Name) &&
                             x.Name.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Surname) &&
                             x.Surname.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.OverallPlace.ToString()) &&
                             x.OverallPlace.ToString().StartsWith(filterText, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return searchedPlayers;
        }

        public async Task<List<PlayerDB>> GetMenPlayers()
        {
            var players = await _databaseService.GetPlayersAsync();
            var list = players
                .Where(x => x.Gender == "male" & x.Place < 5000)
                .OrderBy(x => x.Place)
                .ToList();

            return list;
        }

        public async Task<List<PlayerDB>> GetWomenPlayers()
        {
            var players = await _databaseService.GetPlayersAsync();
            var list = players
                .Where(x => x.Gender == "female" & x.Place < 5000)
                .OrderBy(x => x.Place)
                .ToList();

            return list;
        }

        public async Task<List<PlayerDB>> GetAllPlayers()
        {
            var players = await _databaseService.GetPlayersAsync();
            var list = players
                .Where(x => x.OverallPlace != 0 & x.OverallPlace < 5000)
                .OrderBy(x => x.OverallPlace)
                .ToList();

            return list;
        }

        public async Task<List<PlayerDB>> GetAllInactivePlayers()
        {
            var players = await _databaseService.GetPlayersAsync();
            var list = players
                .Where(x => x.OverallPlace != 0 & x.OverallPlace > 5000)
                .OrderByDescending(x => x.PointsWithBonus)
                .ToList();

            return list;
        }
    }
}
