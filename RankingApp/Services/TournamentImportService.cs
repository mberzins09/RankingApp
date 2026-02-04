using CommunityToolkit.Mvvm.ComponentModel;

namespace RankingApp.Services
{
    public partial class TournamentImportService : ObservableObject
    {
        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        private string _message = "";
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private double _progress;
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public async Task StartImport(Func<IProgress<string>, Task> job)
        {
            if (IsRunning)
            {
                Message = "Import already running…";
                return;
            }

            IsRunning = true;
            Message = "Starting import...";

            var progress = new Progress<string>(msg =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Message = msg;
                });
            });

            await Task.Run(async () =>
            {
                try
                {
                    await job(progress);
                }
                finally
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IsRunning = false;
                        Message = "Finished";
                    });
                }
            });
        }
    }
}
