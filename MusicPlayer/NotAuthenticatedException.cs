using System;

namespace MusicPlayer
{
    [Serializable]
    internal class NotAuthenticatedException : Exception
    {
        public NotAuthenticatedException() : this("You could not get Authenticated.")
        {
        }

        public NotAuthenticatedException(string message) : base(message)
        {
        }

        public NotAuthenticatedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}