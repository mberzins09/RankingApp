using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace RankingApp.Models
{
    public partial class Game : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private int myPoints;


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MyFullName))]
        [NotifyPropertyChangedFor(nameof(GameDisplayPlayers))]
        private string myName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MyFullName))]
        [NotifyPropertyChangedFor(nameof(GameDisplayPlayers))]
        private string mySurname;

        public string MyFullName => $"{MyName} {MySurname}";


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private int opponentPoints;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OpponentName))]
        [NotifyPropertyChangedFor(nameof(GameDisplayPlayers))]
        private string? name;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OpponentName))]
        [NotifyPropertyChangedFor(nameof(GameDisplayPlayers))]
        private string? surname;

        public string? OpponentName => $"{Name} {Surname}";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GameDisplayDetails))]
        [NotifyPropertyChangedFor(nameof(Gamescore))]
        [NotifyPropertyChangedFor(nameof(IsWin))]
        [NotifyPropertyChangedFor(nameof(GameDisplayPlayers))]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private int? mySets;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GameDisplayDetails))]
        [NotifyPropertyChangedFor(nameof(Gamescore))]
        [NotifyPropertyChangedFor(nameof(IsWin))]
        [NotifyPropertyChangedFor(nameof(GameDisplayPlayers))]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private int? opponentSets;

        public bool IsWin => (MySets ?? 0) > (OpponentSets ?? 0);

        public int TournamentId { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GameDisplayDetails))]
        [NotifyPropertyChangedFor(nameof(GameName))]
        private string? tournamentName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GameDisplayDetails))]
        [NotifyPropertyChangedFor(nameof(GameName))]
        [NotifyPropertyChangedFor(nameof(DateToString))]
        private DateTime tournamentDate;

        public string DateToString => TournamentDate.ToString("d MMM yyyy");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private string gameCoefficient;

        public string Gamescore => $"{MySets} : {OpponentSets}";

        public string GameName => $"{TournamentName} - {DateToString}";

        public string GameDisplayPlayers => $"{MyFullName} - {OpponentName} {Gamescore}";

        public string GameDisplayDetails => $"{GameName} : {RatingDifference}";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GameDisplayDetails))]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private bool isOpponentForeign;

        public int RatingDifference
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Surname) ||
                    MySets is null || OpponentSets is null)
                {
                    return 0;
                }

                return IsOpponentForeign
                    ? 0
                    : RatingCalculator.Calculate(MyPoints, OpponentPoints, IsWin, GameCoefficient);
            }
        }

        [ObservableProperty]
        private int myPointsWithBonus;

        [ObservableProperty]
        private int opponentPointsWithBonus;

        [ObservableProperty]
        private int myAge;

        [ObservableProperty]
        private int opponentAge;
        
        [ObservableProperty]
        private int myPlace;

        [ObservableProperty]
        private int opponentPlace;
    }
}
