using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace RankingApp.Models
{
    public abstract class Entity : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
    }
}
