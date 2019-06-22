using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Media.Playback;

namespace MusicPlayer.Controls
{
    internal sealed class MediaBehaviorCommand : ICommand, IDisposable
    {
        private MediaPlaybackCommandManagerCommandBehavior behavior;
        private readonly Action onExecute;

        public MediaBehaviorCommand(MediaPlaybackCommandManagerCommandBehavior behavior, Action onExecute)
        {
            this.behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            this.onExecute = onExecute ?? throw new ArgumentNullException(nameof(onExecute));
            this.behavior.IsEnabledChanged += this.NextBehavior_IsEnabledChanged;

        }

        private void NextBehavior_IsEnabledChanged(MediaPlaybackCommandManagerCommandBehavior sender, object args)
        {
            Task.Run(() => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty));
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return this.behavior.IsEnabled;
        }

        public void Execute(object parameter)
        {
            this.onExecute();
        }

        public void Dispose()
        {
            this.behavior.IsEnabledChanged -= this.NextBehavior_IsEnabledChanged;

        }
    }

    public interface IToggleStateCommand : ICommand
    {
        bool State { get; }
        bool IsEnabled { get; }
    }

    internal sealed class MediaBehaviorWithStateCommand : Windows.UI.Xaml.DependencyObject, IToggleStateCommand, IDisposable, INotifyPropertyChanged
    {
        private readonly MediaPlaybackCommandManagerCommandBehavior behavior;
        private readonly Func<bool> onExecuteWithNewState;

        public bool State { get; private set; }
        public bool IsEnabled { get; private set; }

        public MediaBehaviorWithStateCommand(MediaPlaybackCommandManagerCommandBehavior behavior, Func<bool> onExecuteWithNewState, bool initialState)
        {
            this.behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            this.onExecuteWithNewState = onExecuteWithNewState ?? throw new ArgumentNullException(nameof(onExecuteWithNewState));
            this.behavior.IsEnabledChanged += this.NextBehavior_IsEnabledChanged;
            this.IsEnabled = this.behavior.IsEnabled;
        }

        private async void NextBehavior_IsEnabledChanged(MediaPlaybackCommandManagerCommandBehavior sender, object args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
             {
                 this.IsEnabled = this.behavior.IsEnabled;
                 this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                 this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsEnabled)));
             });
        }

        public event EventHandler CanExecuteChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool CanExecute(object parameter)
        {
            return this.behavior.IsEnabled;
        }

        public void Execute(object parameter)
        {
            var newState = this.onExecuteWithNewState();
            this.State = newState;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.State)));
        }

        public void Dispose()
        {
            this.behavior.IsEnabledChanged -= this.NextBehavior_IsEnabledChanged;

        }
    }


}