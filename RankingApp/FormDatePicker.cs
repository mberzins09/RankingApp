namespace RankingApp
{
    /// <summary>
    /// DatePicker that always reports a change, even when the same date is picked again
    /// (it sets the day before, then the real date).
    /// MAUI 10: IDatePicker.Date is DateTime? (a picker can be empty).
    /// </summary>
    public partial class FormDatePicker : Microsoft.Maui.Controls.DatePicker, IDatePicker
    {
        DateTime? IDatePicker.Date
        {
            get => Date;
            set
            {
                // Our pages always bind a real date (tournament date, ranking month) - never clear it
                if (value is not DateTime newDate)
                    return;

                if (newDate.Equals(Date))
                    Date = newDate.AddDays(-1);

                Date = newDate;
                OnPropertyChanged(nameof(Date));
            }
        }
    }
}
