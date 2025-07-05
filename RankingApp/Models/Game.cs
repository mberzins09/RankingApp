using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SQLite;

namespace RankingApp.Models
{
    public class Game : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        private int _myPoints;
        public int MyPoints
        {
            get => _myPoints;
            set
            {
                if (_myPoints != value)
                {
                    _myPoints = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RatingDifference));
                }
            }
        }

        private string _myName;
        public string MyName
        {
            get => _myName;
            set
            {
                if (_myName != value)
                {
                    _myName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MyFullName));
                    OnPropertyChanged(nameof(GameDisplayPlayers));
                }
            }
        }

        private string _mySurname;
        public string MySurname
        {
            get => _mySurname;
            set
            {
                if (_mySurname != value)
                {
                    _mySurname = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MyFullName));
                    OnPropertyChanged(nameof(GameDisplayPlayers));
                }
            }
        }

        public string MyFullName => $"{MyName} {MySurname}";

        private int _opponentPoints;
        public int OpponentPoints
        {
            get => _opponentPoints;
            set
            {
                if (_opponentPoints != value)
                {
                    _opponentPoints = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RatingDifference));
                }
            }
        }

        private string? _name;
        public string? Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(OpponentName));
                    OnPropertyChanged(nameof(GameDisplayPlayers));
                }
            }
        }

        private string? _surname;
        public string? Surname
        {
            get => _surname;
            set
            {
                if (_surname != value)
                {
                    _surname = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(OpponentName));
                    OnPropertyChanged(nameof(GameDisplayPlayers));
                }
            }
        }

        public string? OpponentName => $"{Name} {Surname}";

        private int? _mySets;
        public int? MySets
        {
            get => _mySets;
            set
            {
                if (_mySets != value)
                {
                    _mySets = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GameScore));
                    OnPropertyChanged(nameof(IsWin));
                    OnPropertyChanged(nameof(GameDisplayPlayers));
                    OnPropertyChanged(nameof(GameDisplayDetails));
                }
            }
        }

        private int? _opponentSets;
        public int? OpponentSets
        {
            get => _opponentSets;
            set
            {
                if (_opponentSets != value)
                {
                    _opponentSets = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GameScore));
                    OnPropertyChanged(nameof(IsWin));
                    OnPropertyChanged(nameof(GameDisplayPlayers));
                    OnPropertyChanged(nameof(GameDisplayDetails));
                }
            }
        }

        public bool IsWin => (MySets ?? 0) > (OpponentSets ?? 0);

        public int TournamentId { get; set; }

        private string? _tournamentName;
        public string? TournamentName
        {
            get => _tournamentName;
            set
            {
                if (_tournamentName != value)
                {
                    _tournamentName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GameName));
                    OnPropertyChanged(nameof(GameDisplayDetails));
                }
            }
        }

        private DateTime _tournamentDate;
        public DateTime TournamentDate
        {
            get => _tournamentDate;
            set
            {
                if (_tournamentDate != value)
                {
                    _tournamentDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DateToString));
                    OnPropertyChanged(nameof(GameName));
                    OnPropertyChanged(nameof(GameDisplayDetails));
                }
            }
        }

        public string DateToString => TournamentDate.ToString("d MMM yyyy");

        private string _gameCoefficient;
        public string GameCoefficient
        {
            get => _gameCoefficient;
            set
            {
                if (_gameCoefficient != value)
                {
                    _gameCoefficient = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RatingDifference));
                }
            }
        }

        public string GameScore => $"{MySets} : {OpponentSets}";

        public string GameName => $"{TournamentName} - {DateToString}";

        public string GameDisplayPlayers => $"{MyFullName} - {OpponentName} {GameScore}";

        public string GameDisplayDetails => $"{GameName} : {RatingDifference}";

        private bool _isOpponentForeign;
        public bool IsOpponentForeign
        {
            get => _isOpponentForeign;
            set
            {
                if (_isOpponentForeign != value)
                {
                    _isOpponentForeign = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RatingDifference));
                    OnPropertyChanged(nameof(GameDisplayDetails));
                }
            }
        }

        public int RatingDifference => IsOpponentForeign ? 0 : RatingCalculator.Calculate(MyPoints, OpponentPoints, IsWin, GameCoefficient);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
