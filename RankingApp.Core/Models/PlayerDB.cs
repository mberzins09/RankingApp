using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace RankingApp.Core.Models
{
    public partial class PlayerDB : Entity
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Display))]
        private int place;

        [ObservableProperty]
        private int points;

        [ObservableProperty]
        private int pointsWithBonus;

        [ObservableProperty]
        private int pointsChanged;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Display))]
        private string name;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Display))]
        private string surname;

        [ObservableProperty]
        private string gender;

        [ObservableProperty]
        private int overallPlace;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Age))]
        private string birthDate;

        public bool IsActive { get; set; }

        public int NewId { get; set; }

        [Indexed]
        public string KeyName { get; set; }

        public int Age => string.IsNullOrEmpty(BirthDate) ? 0 : AgeCalculator.Calculate(BirthDate);

        public string Display => $"{Name} {Surname} {Age} g";
    }
}