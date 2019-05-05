using System;
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

   
}