using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace RankingApp.Core.Models
{
    public abstract class Entity : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
    }
}
