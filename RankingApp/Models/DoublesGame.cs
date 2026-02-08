using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace RankingApp.Models
{
    public partial class DoublesGame : Entity, IGame
    {
        // My team
        [ObservableProperty]
        private string myName;

        [ObservableProperty]
        private string mySurname;

        [ObservableProperty]
        private int myPoints;

        [ObservableProperty]
        private int myPointsWithBonus;

        [ObservableProperty]
        private int myPlace;

        [ObservableProperty]
        private int myAge;

        [ObservableProperty]
        private string myPartnerName;

        [ObservableProperty]
        private string myPartnerSurname;

        [ObservableProperty]
        private int myPartnerPoints;

        [ObservableProperty]
        private int myPartnerPointsWithBonus;

        [ObservableProperty]
        private int myPartnerAge;

        [ObservableProperty]
        private int myPartnerPlace;

        // Opponent team
        [ObservableProperty]
        private string opponent1Name;

        [ObservableProperty]
        private string opponent1Surname;

        [ObservableProperty]
        private int opponent1Points;

        [ObservableProperty]
        private int opponent1PointsWithBonus;

        [ObservableProperty]
        private int opponent1Place;

        [ObservableProperty]
        private int opponent1Age;

        [ObservableProperty]
        private string opponent2Name;

        [ObservableProperty]
        private string opponent2Surname;

        [ObservableProperty]
        private int opponent2Points;

        [ObservableProperty]
        private int opponent2PointsWithBonus;

        [ObservableProperty]
        private int opponent2Place;

        [ObservableProperty]
        private int opponent2Age;

        // Sets
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Gamescore))]
        [NotifyPropertyChangedFor(nameof(IsWin))]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private int? mySets;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Gamescore))]
        [NotifyPropertyChangedFor(nameof(IsWin))]
        [NotifyPropertyChangedFor(nameof(RatingDifference))]
        private int? opponentSets;

        // Tournament info
        public int TournamentId { get; set; }

        public bool IsWin => (MySets ?? 0) > (OpponentSets ?? 0);

        [ObservableProperty]
        private string? tournamentName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GameDate))]
        private DateTime tournamentDate;

        [ObservableProperty]
        private string gameCoefficient;

        // Calculated properties
        public string MyTeam => $"{MyName[0]}.{MySurname}/{MyPartnerName[0]}.{MyPartnerSurname}";
        public string OpponentTeam => $"{Opponent1Name[0]}.{Opponent1Surname}/{Opponent2Name[0]}.{Opponent2Surname}";
        public string Gamescore => $"{MySets} : {OpponentSets}";
        public string GameDate => $"{TournamentDate:d MMM yyyy}";
        public string GameDisplayMe => $"{MyTeam}";
        public string GameDisplayOpp => $"{OpponentTeam}";
        public int MyTeamPoints => MyPoints + MyPartnerPoints;
        public int OpponentPoints => Opponent1Points + Opponent2Points;
        public string FirstOppKeyName => NameNormalizer.NormalizeKey($"{Opponent1Name} {Opponent1Surname}");
        public string SecondOppKeyName => NameNormalizer.NormalizeKey($"{Opponent2Name} {Opponent2Surname}");

        public int RatingDifference
        {
            get
            {
                if (MySets is null || OpponentSets is null)
                {
                    return 0;
                }

                return RatingCalculator.Calculate(MyPoints + MyPartnerPoints, Opponent1Points + Opponent2Points, IsWin, GameCoefficient);
            }
        }
    }
}
