using System;
using Windows.UI.Xaml.Data;

namespace MusicPlayer.Converters
{
    public class AddCoverter : IValueConverter
    {
        public int Precision { get; set; } = 2;

        public object Convert(object value, Type targetType, object parameter, string language)
        {

            if (value is double d)
            {
                var toadd = System.Convert.ToDouble(parameter);
                d += toadd;
                return d;
            }
            throw new NotImplementedException();

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
