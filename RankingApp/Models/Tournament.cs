using System.ComponentModel;
using SQLite;

namespace RankingApp.Models
{
    public class Tournament : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        private string _coefficient;
        public string Coefficient
        {
            get => _coefficient;
            set
            {
                if (_coefficient != value)
                {
                    _coefficient = value;
                    OnPropertyChanged(nameof(Coefficient));
                }
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set
            {
                if (_date != value)
                {
                    _date = value;
                    OnPropertyChanged(nameof(Date));
                    OnPropertyChanged(nameof(DateToString)); // Because it depends on Date
                }
            }
        }

        public string DateToString => Date.ToString("d MMM yyyy");

        private string _tournamentPlayerName;
        public string TournamentPlayerName
        {
            get => _tournamentPlayerName;
            set
            {
                if (_tournamentPlayerName != value)
                {
                    _tournamentPlayerName = value;
                    OnPropertyChanged(nameof(TournamentPlayerName));
                    OnPropertyChanged(nameof(TournamentDisplay));
                }
            }
        }

        private string _tournamentPlayerSurname;
        public string TournamentPlayerSurname
        {
            get => _tournamentPlayerSurname;
            set
            {
                if (_tournamentPlayerSurname != value)
                {
                    _tournamentPlayerSurname = value;
                    OnPropertyChanged(nameof(TournamentPlayerSurname));
                    OnPropertyChanged(nameof(TournamentDisplay));
                }
            }
        }

        public int TournamentPlayerPoints { get; set; }
        public int PointsDifference { get; set; }
        public int TournamentPlayerId { get; set; }

        public string TournamentDisplay =>
            $"{TournamentPlayerName} {TournamentPlayerSurname} - {DateToString} : {PointsDifference}";
    }
}
