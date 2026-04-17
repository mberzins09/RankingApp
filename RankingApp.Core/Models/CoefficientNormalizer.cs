namespace RankingApp.Core.Models
{
    public static class CoefficientNormalizer
    {
        public static string Normalize(string? coef)
        {
            if (string.IsNullOrWhiteSpace(coef))
                return "0";

            return coef switch
            {
                "0.00" => "0",
                "0.0" => "0",
                "0.25" => "0.25",
                "0.50" => "0.5",
                "0.5" => "0.5",
                "1.00" => "1",
                "1.0" => "1",
                "1.50" => "1.5",
                "2.00" => "2",
                "2.0" => "2",
                "4.00" => "4",
                "4.0" => "4",
                _ => coef
            };
        }
    }
}
