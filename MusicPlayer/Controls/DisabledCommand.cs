using System;
using System.Windows.Input;

namespace MusicPlayer.Controls
{
    internal class DisabledCommand : ICommand
    {

        public static ICommand Instance { get; } = new DisabledCommand();

        public bool IsEnabled => false;

        private DisabledCommand()
        {
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object parameter) => false;

        public void Execute(object parameter)
        {
        }
    }
}