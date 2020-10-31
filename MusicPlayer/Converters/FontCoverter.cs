using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace MusicPlayer.Converters
{
    public class FontCoverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {

            if (value is char c)
            {
                if(c >= '\uE700'
                    && c <= '\uF847')
                {
                    return new FontFamily("Segoe MDL2 Assets");
                }
                return null;
            }
            throw new NotImplementedException();

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
