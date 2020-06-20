using System;
using Windows.UI;
using Windows.UI.Xaml.Data;

namespace MusicPlayer.Converters
{
    public class ToTransparent : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Color color)
            {
                var transparent = 0.0;
                try
                {
                    transparent = System.Convert.ToDouble(parameter);

                }
                catch (FormatException) { }
                catch (InvalidCastException) { }
                color.A = (byte)(255* transparent);
                return color;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
