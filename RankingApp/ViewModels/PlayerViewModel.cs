using System.Collections.ObjectModel;
using RankingApp.Models;
using RankingApp.Services;

namespace RankingApp.ViewModels
{
    public class PlayerViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        public PlayerViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _ = LoadPlayersAsyncFromDatabase();
        }

        public ObservableCollection<PlayerDB> Mens { get; set; }
        public ObservableCollection<PlayerDB> Womens { get; set; }
        public ObservableCollection<PlayerDB> Players { get; set; }
        public List<PlayerDB> MensList { get; set; } = new List<PlayerDB>();
        public List<PlayerDB> WomensList { get; set; } = new List<PlayerDB>();
        public List<PlayerDB> PlayersList { get; set; } = new List<PlayerDB>();

        private async Task LoadPlayersAsyncFromDatabase()
        {
            var players = await _databaseService.GetPlayersAsync();
            MensList = players
                .Where(x => x.Gender == "male")
                .OrderByDescending(x => x.PointsWithBonus)
                .ToList();

            Mens = new ObservableCollection<PlayerDB>(MensList);

            WomensList = players
                .Where(x => x.Gender == "female")
                .OrderByDescending(x => x.PointsWithBonus)
                .ToList();

            Womens = new ObservableCollection<PlayerDB>(WomensList);

            PlayersList = players
                .Where(x=>x.OverallPlace != 0)
                .OrderByDescending(x=>x.PointsWithBonus)
                .ToList();

            Players = new ObservableCollection<PlayerDB>(PlayersList);

            OnPropertyChanged();
        }

        public List<PlayerDB> SearchPlayers(List<PlayerDB> players, string filterText)
        {
            var searchedPlayers = players
                .Where(x => (!string.IsNullOrWhiteSpace(x.Name) &&
                             x.Name.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Surname) &&
                             x.Surname.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return searchedPlayers;
        }

        public async Task<List<PlayerDB>> GetMenPlayers()
        {
            var players = await _databaseService.GetPlayersAsync();
            var list = players
                .Where(x => x.Gender == "male")
                .OrderByDescending(x => x.PointsWithBonus)
                .ToList();

            return list;
        }

        public async Task<List<PlayerDB>> GetWomenPlayers()
        {
            var players = await _databaseService.GetPlayersAsync();
            var list = players
                .Where(x => x.Gender == "female")
                .OrderByDescending(x => x.PointsWithBonus)
                .ToList();

            return list;
        }

        public async Task<List<PlayerDB>> GetAllPlayers()
        {
            var players = await _databaseService.GetPlayersAsync();
            var list = players
                .Where(x => x.OverallPlace != 0)
                .OrderByDescending(x => x.PointsWithBonus)
                .ToList();

            return list;
        }
    }
}
