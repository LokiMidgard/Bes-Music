using MusicPlayer.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Data;

namespace MusicPlayer.Converters
{
    public delegate void ErrorHandler(Exception exception);

    public class ExceptionHandlerConverter : IValueConverter
    {

        public event ErrorHandler OnError;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ICommand command)
            {
                var c = new Handler(command);
                c.OnError += e => OnError?.Invoke(e);
                return c;
            }
            else
                throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private class Handler : ICommand
        {
            private ICommand command;
            public event ErrorHandler OnError;

            public Handler(ICommand command)
            {
                this.command = command;

            }

            public event EventHandler CanExecuteChanged
            {
                add
                {
                    this.command.CanExecuteChanged += value;
                }
                remove
                {
                    this.command.CanExecuteChanged -= value;
                }
            }

            public bool CanExecute(object parameter) => this.command.CanExecute(parameter);

            public async void Execute(object parameter)
            {
                try
                {
                    if (this.command is DelegateCommand delegateCommand)
                        await delegateCommand.Execute(parameter);
                    else
                        this.command.Execute(parameter);
                }
                catch (Exception e)
                {
                    OnError?.Invoke(e);
                }
            }
        }
    }
}
