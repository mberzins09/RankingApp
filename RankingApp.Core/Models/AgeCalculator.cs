namespace RankingApp.Core.Models
{
    public static class AgeCalculator
    {
        public static DateTime now = DateTime.Now;

        public static int CalculateAge(string BirthDate, DateTime date)
        {
            DateTime birth;

            if (!DateTime.TryParse(BirthDate, out birth))
            {
                return 0;
            }

            int age = date.Year - birth.Year;
            if (birth.AddYears(age) > date)
            {
                age--;
            }

            return age;
        }

        public static int Calculate(string BirthDate)
        {
            return CalculateAge(BirthDate, now);
        }
    }
}
