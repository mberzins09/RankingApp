namespace RankingApp.Core.ViewModels
{
    public interface ISaveBeforeNavigate
    {
        Task<bool> SaveBeforeNavigateAsync();
    }
}
