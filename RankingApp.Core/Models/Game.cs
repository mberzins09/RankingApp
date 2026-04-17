using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace RankingApp.Core.Models
{
    public partial class Game : Entity, IGame
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private int myPoints;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MyFullName))]
        [NotifyPropertyChangedFor(nameof(GameDisplayMe))]
        private string myName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MyFullName))]
        [NotifyPropertyChangedFor(nameof(GameDisplayMe))]
        private string mySurname;

        public string MyFullName => $"{MyName} {MySurname}";


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private int opponentPoints;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OpponentName))]
        [NotifyPropertyChangedFor(nameof(GameDisplayOpp))]
        private string? name;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OpponentName))]
        [NotifyPropertyChangedFor(nameof(GameDisplayOpp))]
        private string? surname;

        public string? OpponentName => $"{Name} {Surname}";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Gamescore))]
        [NotifyPropertyChangedFor(nameof(IsWin))]
        [NotifyPropertyChangedFor(nameof(GameDisplayMe))]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private int? mySets;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Gamescore))]
        [NotifyPropertyChangedFor(nameof(IsWin))]
        [NotifyPropertyChangedFor(nameof(GameDisplayOpp))]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private int? opponentSets;

        public bool IsWin => (MySets ?? 0) > (OpponentSets ?? 0);

        public int TournamentId { get; set; }

        [ObservableProperty]
        private string? tournamentName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GameDate))]
        [NotifyPropertyChangedFor(nameof(DateToString))]
        private DateTime tournamentDate;

        public string DateToString => TournamentDate.ToString("d MMM yyyy");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private string gameCoefficient;

        public string Gamescore => $"{MySets} : {OpponentSets}";

        public string GameDate => $"{TournamentDate:d MMM yyyy}";

        public string GameDisplayMe => $"{MyFullName}({MyPlace})";
        public string GameDisplayOpp => $"{OpponentName}({OpponentPlace})";
        public string OppKeyName => NameNormalizer.NormalizeKey($"{Name} {Surname}");

        [ObservableProperty]
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
        [NotifyPropertyChangedFor(nameof(GameDisplayMe))]
        private int myPlace;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GameDisplayOpp))]
        private int opponentPlace;

        public int ExternalGameId { get; set; }
    }
}
