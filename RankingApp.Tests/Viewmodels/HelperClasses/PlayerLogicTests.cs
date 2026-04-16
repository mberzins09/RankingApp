using System;
using System.Collections.Generic;
using System.Linq;
using RankingApp.Core.Models;
using RankingApp.Core.ViewModels.HelperClasses;

namespace RankingApp.Tests.Viewmodels.HelperClasses
{
    public class PlayerLogicTests
    {
        [Fact]
        public void Search_Range_ShouldReturnCorrectPlayers()
        {
            var players = CreatePlayers(10);

            var result = PlayerLogic.Search(players, "1-3");

            Assert.Equal(3, result.Count);
            Assert.All(result, p => Assert.InRange(p.Place, 1, 3));
        }

        [Fact]
        public void Search_GreaterThan_ShouldWork()
        {
            var players = CreatePlayers(10);

            var result = PlayerLogic.Search(players, ">5");

            Assert.All(result, p => Assert.True(p.Place > 5));
        }

        [Fact]
        public void Search_LessOrEqual_ShouldWork()
        {
            var players = CreatePlayers(10);

            var result = PlayerLogic.Search(players, "<=3");

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void Search_ByName_ShouldWork()
        {
            var players = new List<PlayerListItem>
    {
        new() { Place = 1, Player = new PlayerDB { KeyName = "janisberzins" } },
        new() { Place = 2, Player = new PlayerDB { KeyName = "peteriskalns" } }
    };

            var result = PlayerLogic.Search(players, "janis");

            Assert.Single(result);
            Assert.Equal(1, result.First().Place);
        }

        [Fact]
        public void Search_Empty_ShouldReturnAll()
        {
            var players = CreatePlayers(5);

            var result = PlayerLogic.Search(players, "");

            Assert.Equal(5, result.Count);
        }

        private List<PlayerListItem> CreatePlayers(int count)
        {
            return Enumerable.Range(1, count)
                .Select(i => new PlayerListItem
                {
                    Place = i,
                    Player = new PlayerDB
                    {
                        KeyName = $"player{i}"
                    }
                })
                .ToList();
        }
    }
}
