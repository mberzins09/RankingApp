namespace RankingApp.Core.Interfaces
{
    public interface IProgressDialogService
    {
        void Show(string message);
        void Update(string message);
        Task HideAsync();
    }
}
