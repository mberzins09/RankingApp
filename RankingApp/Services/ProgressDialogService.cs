using CommunityToolkit.Maui.Extensions;
using RankingApp.Core.Interfaces;
using RankingApp.Views;

namespace RankingApp.Services
{
    /// <summary>
    /// One "processing" popup shared by everyone who asks for it.
    ///  - A second Show while the popup is open only changes the text (no second popup that
    ///    would take over Update/Hide and leave the first one open forever).
    ///  - The popup closes when the last caller has called HideAsync.
    ///  - HideAsync waits until the popup has really opened before closing it (Show does not wait).
    /// </summary>
    public class ProgressDialogService : IProgressDialogService
    {
        private ProcessingPopup? _popup;
        private TaskCompletionSource? _opened;
        private int _users;

        public void Show(string message)
        {
            _users++;

            if (_popup != null)
            {
                Update(message);
                return;
            }

            var popup = new ProcessingPopup { Message = message };
            var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            popup.Loaded += (_, _) => opened.TrySetResult();

            _popup = popup;
            _opened = opened;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var page = Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page;

                    if (page == null)
                    {
                        opened.TrySetResult();
                        return;
                    }

                    // Completes when the popup is closed - not awaited by Show on purpose
                    await page.ShowPopupAsync(popup);
                }
                catch
                {
                    // Could not show it - never block HideAsync
                    opened.TrySetResult();
                }
            });
        }

        public void Update(string message)
        {
            var popup = _popup;
            if (popup == null)
                return;

            if (MainThread.IsMainThread)
                popup.Message = message;
            else
                MainThread.BeginInvokeOnMainThread(() => popup.Message = message);
        }

        public async Task HideAsync()
        {
            _users = Math.Max(0, _users - 1);

            if (_users > 0)
                return; // someone else still uses the popup

            var popup = _popup;
            var opened = _opened;

            _popup = null;
            _opened = null;

            if (popup == null)
                return;

            if (opened != null)
                await Task.WhenAny(opened.Task, Task.Delay(3000));

            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => popup.CloseAsync());
            }
            catch
            {
                // Already closed / never shown - nothing left to close
            }
        }
    }
}
