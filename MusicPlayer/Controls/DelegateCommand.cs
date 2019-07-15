using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MusicPlayer.Controls
{
    internal class DelegateCommand : ICommand
    {
        private readonly Func<Task> onExecute;
        private readonly Func<bool> onCanExecute;

        private protected DelegateCommand()
        {

        }

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

        public virtual bool CanExecute(object parameter) => this.onCanExecute?.Invoke() ?? true;

        public virtual Task Execute(object parameter) => this.onExecute();

        internal void FireCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);


        void ICommand.Execute(object parameter)
        {
            this.Execute(parameter);
        }
    }


    internal class DelegateCommand<T> : DelegateCommand
    {
        private readonly Func<T, Task> onExecute;
        private readonly Func<bool> onCanExecute;

        public DelegateCommand(Action<T> onExecute, Func<bool> onCanExecute = null)
        {
            this.onExecute = t => { onExecute(t); return Task.CompletedTask; };
            this.onCanExecute = onCanExecute;
        }
        public DelegateCommand(Func<T, Task> onExecute, Func<bool> onCanExecute = null)
        {
            this.onExecute = onExecute;
            this.onCanExecute = onCanExecute;
        }


        public override bool CanExecute(object parameter) => this.onCanExecute?.Invoke() ?? true;

        public override Task Execute(object parameter) => this.onExecute((T)parameter);

    }
}