namespace RankingApp.Models
{
    public static class AgeCalculator
    {
        public static DateTime now = DateTime.Now;

        public static int Calculate(string BirthDate)
        {
            DateTime birth = DateTime.Parse(BirthDate);
            int age = now.Year - birth.Year;
            if(birth.AddYears(age) > now)
            {
                age--;
            }

            return age;
        }
    }
}
