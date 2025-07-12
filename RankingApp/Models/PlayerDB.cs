using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace RankingApp.Models
{
    public partial class PlayerDB : ObservableObject
    {
        [PrimaryKey]
        public int Id { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Display))]
        private int place;

        [ObservableProperty]
        private int points;

        [ObservableProperty]
        private int pointsWithBonus;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Display))]
        [NotifyPropertyChangedFor(nameof(AllDisplay))]
        private string name;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Display))]
        [NotifyPropertyChangedFor(nameof(AllDisplay))]
        private string surname;

        [ObservableProperty]
        private string gender;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AllDisplay))]
        private int overallPlace;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Age))]
        private string birthDate;

        public int Age => string.IsNullOrEmpty(BirthDate) ? 0 : AgeCalculator.Calculate(BirthDate);

        public string Display => $"{Place}. {Name} {Surname} {Age} g";
        public string AllDisplay => $"{OverallPlace}. {Name} {Surname} {Age} g";
    }
}