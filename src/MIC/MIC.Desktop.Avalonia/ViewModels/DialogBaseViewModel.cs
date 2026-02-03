using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace MIC.Desktop.Avalonia.ViewModels
{
    public class DialogBaseViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, Unit> CloseDialogCommand { get; }
        public event Action? RequestClose;

        public DialogBaseViewModel()
        {
            CloseDialogCommand = ReactiveCommand.Create(() => RequestClose?.Invoke());
        }
    }
}
