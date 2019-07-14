using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MusicPlayer.Controls
{
    internal class DelegateCommand : ICommand
    {
        private readonly Func<Task> onExecute;
        private readonly Func<bool> onCanExecute;

        public DelegateCommand(Action onExecute, Func<bool> onCanExecute = null)
        {
            this.onExecute = () => { onExecute(); return Task.CompletedTask; };
            this.onCanExecute = onCanExecute;
        }
        public DelegateCommand(Func<Task> onExecute, Func<bool> onCanExecute = null)
        {
            this.onExecute = onExecute;
            this.onCanExecute = onCanExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => this.onCanExecute?.Invoke() ?? true;

        public Task Execute(object parameter) => this.onExecute();

        internal void FireCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);


        void ICommand.Execute(object parameter)
        {
            this.Execute(parameter);
        }
    }
}