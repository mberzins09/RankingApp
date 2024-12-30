namespace RankingApp.Models
{
    public static class RatingCalculator
    {
        public static int Calculate(int MyPoints, int OppPoints, bool isWin, string c)
        {
            int rD = 0;
            int pD = MyPoints - OppPoints;
            float coef;
            switch (c)
            {
                case "0":
                    coef = (float)0;
                    break;
                case "0.25":
                    coef = 0.25f;
                    break;
                case "0.5":
                    coef = 0.5f;
                    break;
                case "1":
                    coef = (float)1;
                    break;
                case "1.5":
                    coef = 1.5f;
                    break;
                case "2":
                    coef = (float)2;
                    break;
                case "4":
                    coef = (float)4;
                    break;
                default:
                    coef = (float)0;
                    break;
            }
            
            if (isWin)
            {
                if (pD > 0)
                {
                    if (pD >= 1 && pD <= 25) { rD = (int)Math.Ceiling(coef * 9); }
                    if (pD >= 26 && pD <= 50) { rD = (int)Math.Ceiling(coef * 8); }
                    if (pD >= 51 && pD <= 100) { rD = (int)Math.Ceiling(coef * 7); }
                    if (pD >= 101 && pD <= 150) { rD = (int)Math.Ceiling(coef * 6); }
                    if (pD >= 151 && pD <= 200) { rD = (int)Math.Ceiling(coef * 5); }
                    if (pD >= 201 && pD <= 300) { rD = (int)Math.Ceiling(coef * 4); }
                    if (pD >= 301 && pD <= 400) { rD = (int)Math.Ceiling(coef * 3); }
                    if (pD >= 401 && pD <= 500) { rD = (int)Math.Ceiling(coef * 2); }
                    if (pD >= 501 ) { rD = (int)Math.Ceiling(coef * 1); }
                }

                else
                {
                    pD = Math.Abs(pD);

                    if (pD >= 0 && pD <= 24) { rD = (int)Math.Ceiling(coef * 10); }
                    if (pD >= 25 && pD <= 49) { rD = (int)Math.Ceiling(coef * 11); }
                    if (pD >= 50 && pD <= 99) { rD = (int)Math.Ceiling(coef * 13); }
                    if (pD >= 100 && pD <= 149) { rD = (int)Math.Ceiling(coef * 15); }
                    if (pD >= 150 && pD <= 199) { rD = (int)Math.Ceiling(coef * 18); }
                    if (pD >= 200 && pD <= 299) { rD = (int)Math.Ceiling(coef * 21); }
                    if (pD >= 300 && pD <= 399) { rD = (int)Math.Ceiling(coef * 24); }
                    if (pD >= 400 && pD <= 499) { rD = (int)Math.Ceiling(coef * 28); }
                    if (pD >= 500) { rD = (int)Math.Ceiling(coef * 32); }
                }

                return rD;
            }

            else
            {
                if (pD > 0)
                {
                    if (pD >= 0 && pD <= 24) { rD = -(int)Math.Floor(coef * 9); }
                    if (pD >= 25 && pD <= 49) { rD = -(int)Math.Floor(coef * 11); }
                    if (pD >= 50 && pD <= 99) { rD = -(int)Math.Floor(coef * 11); }
                    if (pD >= 100 && pD <= 149) { rD = -(int)Math.Floor(coef * 12); }
                    if (pD >= 150 && pD <= 199) { rD = -(int)Math.Floor(coef * 14); }
                    if (pD >= 200 && pD <= 299) { rD = -(int)Math.Floor(coef * 16); }
                    if (pD >= 300 && pD <= 399) { rD = -(int)Math.Floor(coef * 18); }
                    if (pD >= 400 && pD <= 499) { rD = -(int)Math.Floor(coef * 21); }
                    if (pD >= 500) { rD = -(int)Math.Floor(coef * 25); }
                }

                else
                {
                    pD = Math.Abs(pD);

                    if (pD >= 1 && pD <= 25) { rD = -(int)Math.Floor(coef * 8); }
                    if (pD >= 26 && pD <= 50) { rD = -(int)Math.Floor(coef * 7); }
                    if (pD >= 51 && pD <= 100) { rD = -(int)Math.Floor(coef * 6); }
                    if (pD >= 101 && pD <= 150) { rD = -(int)Math.Floor(coef * 5); }
                    if (pD >= 151 && pD <= 200) { rD = -(int)Math.Floor(coef * 4); }
                    if (pD >= 201 && pD <= 300) { rD = -(int)Math.Floor(coef * 3); }
                    if (pD >= 301 && pD <= 400) { rD = -(int)Math.Floor(coef * 2); }
                    if (pD >= 401 && pD <= 500) { rD = -(int)Math.Floor(coef * 1); }
                    if (pD >= 501) { rD = 0; }
                }

                return rD;
            }
        }
    }
}
