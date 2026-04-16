using CommunityToolkit.Maui.Extensions;
using RankingApp.Core.Interfaces;
using RankingApp.Views;

namespace RankingApp.Services
{
    public class ProgressDialogService : IProgressDialogService
    {
        private ProcessingPopup? _popup;

        public async void Show(string message)
        {
            _popup = new ProcessingPopup
            {
                Message = message
            };

            await Application.Current!.MainPage!.ShowPopupAsync(_popup);
        }

        public void Update(string message)
        {
            if (_popup != null)
                _popup.Message = message;
        }

        public async Task HideAsync()
        {
            if (_popup != null)
            {
                await _popup.CloseAsync();
                _popup = null;
            }
        }
    }
}
