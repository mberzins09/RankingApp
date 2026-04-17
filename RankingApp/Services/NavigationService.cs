using RankingApp.Core.Interfaces;

namespace RankingApp.Services
{
    public class NavigationService : INavigationService
    {
        public Task GoToAsync(string route)
        {
            return Shell.Current.GoToAsync(route);
        }
    }
}
