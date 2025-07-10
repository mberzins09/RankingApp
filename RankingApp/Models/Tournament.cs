using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System.ComponentModel;

namespace RankingApp.Models
{
    public partial class Tournament : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [ObservableProperty]
        private string coefficient;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DateToString))]
        private DateTime date;

        public string DateToString => Date.ToString("d MMM yyyy");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TournamentDisplay))]
        private string tournamentPlayerName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TournamentDisplay))]
        private string tournamentPlayerSurname;

        public int TournamentPlayerPoints { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TournamentDisplay))]
        private int pointsDifference;
        
        public int TournamentPlayerId { get; set; }

        public string TournamentDisplay =>
            $"{TournamentPlayerName} {TournamentPlayerSurname} - {DateToString} : {PointsDifference}";
    }
}
