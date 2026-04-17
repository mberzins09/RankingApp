namespace RankingApp.Core.Interfaces
{
    public interface INavigationService
    {
        Task GoToAsync(string route);
    }
}
