using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RankingApp.Models;

namespace RankingApp.Controllers
{
    public class PlayerViewModel : INotifyPropertyChanged
    {
        private readonly PlayerService _dataService;

        public ObservableCollection<Player> Womens { get; set; } = new ObservableCollection<Player>();
        public ObservableCollection<Player> Mens { get; set; } = new ObservableCollection<Player>();

        public PlayerViewModel(PlayerService dataService)
        {
            _dataService = dataService;
        }

        public async Task LoadDataWomens()
        {
            var femalePlayers = await _dataService.GetPlayersAsync("sieviete");
            Womens.Clear();
            foreach (var player in femalePlayers)
                Womens.Add(player);
        }

        public async Task LoadDataMens()
        {
            var malePlayers = await _dataService.GetPlayersAsync("virietis");
            Mens.Clear();
            foreach (var player in malePlayers)
                Mens.Add(player);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
