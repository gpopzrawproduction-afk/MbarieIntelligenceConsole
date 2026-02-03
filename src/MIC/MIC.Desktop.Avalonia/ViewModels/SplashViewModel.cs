using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MIC.Desktop.Avalonia.ViewModels
{
    public class SplashViewModel : INotifyPropertyChanged
    {
        private string _loadingMessage = "Initializing...";
        public string LoadingMessage
        {
            get => _loadingMessage;
            set { _loadingMessage = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public async Task RunStartupSequenceAsync()
        {
            var steps = new List<string>
            {
                "Initializing database...",
                "Loading configuration...",
                "Connecting services...",
                "Almost ready..."
            };
            foreach (var step in steps)
            {
                LoadingMessage = step;
                await Task.Delay(700);
            }
        }
    }
}
