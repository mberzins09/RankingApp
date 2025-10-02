using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankingApp.Models
{
    public interface IGame
    {
        int Id { get; }
        int TournamentId { get; }
        string GameDisplayPlayers { get; }
        string GameDisplayDetails { get; }
        string GameName { get; }
        DateTime TournamentDate { get; }
        int RatingDifference { get; }
    }
}
