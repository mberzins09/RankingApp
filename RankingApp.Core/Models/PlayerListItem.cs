using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankingApp.Core.Models
{
    public class PlayerListItem
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
    }
}
