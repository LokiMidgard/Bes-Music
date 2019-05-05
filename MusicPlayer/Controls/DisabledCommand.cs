using System;
using System.Windows.Input;

namespace MusicPlayer.Controls
{
    internal class DisabledCommand : ICommand
    {

        public static ICommand Instance { get; } = new DisabledCommand();

        private DisabledCommand()
        {
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => false;

        public void Execute(object parameter)
        {
        }
    }
}