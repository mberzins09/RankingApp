namespace RankingApp
{
    public class FormDatePicker : Microsoft.Maui.Controls.DatePicker, IDatePicker
    {
        DateTime IDatePicker.Date
        {
            get => Date;
            set
            {
                if (value.Equals(Date))
                    Date = value.AddDays(-1);
                Date = value;
                OnPropertyChanged(nameof(Date));
            }
        }
    }
}
