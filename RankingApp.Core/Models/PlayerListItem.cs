using CommunityToolkit.Mvvm.ComponentModel;

namespace RankingApp.Core.Models
{
    public partial class PlayerListItem : ObservableObject
    {
        public PlayerDB Player { get; set; }

        public int Place { get; set; }

        public string Display =>
            $"{Place}. {Player.Name} {Player.Surname} {Player.Age} g";

        // Two-row card on the Rankings page
        public string PlaceDisplay => $"{Place}.";
        public string FullName => $"{Player.Name} {Player.Surname}".Trim();
        public string AgeDisplay => Player.Age > 0 ? $"{Player.Age} g" : "";

        public int Points => Player.Points;
        public int PointsWithBonus => Player.PointsWithBonus;
        public int PointsChanged => Player.PointsChanged;

        /// <summary>Long-press menu shows "Set as friend" or "Remove friend".</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FriendMenuText), nameof(FriendIcon))]
        private bool isFriend;

        public string FriendMenuText => IsFriend ? "Remove friend" : "Set as friend";
        public string FriendIcon => IsFriend ? "★" : "";
    }
}
