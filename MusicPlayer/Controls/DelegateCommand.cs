using System;
using System.Windows.Input;

namespace MusicPlayer.Controls
{
    internal class DelegateCommand : ICommand
    {
        private readonly Action onExecute;
        private readonly Func<bool> onCanExecute;

        public DelegateCommand(Action onExecute, Func<bool> onCanExecute = null)
        {
            this.onExecute = onExecute;
            this.onCanExecute = onCanExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => this.onCanExecute?.Invoke() ?? true;

        public void Execute(object parameter) => this.onExecute();
    }
}