using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MusicPlayer.Converters
{


    public class StringFormatConverter : IValueConverter
    {
        public string StringFormat { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string format && !string.IsNullOrEmpty(format))
                return string.Format(format, value);
            else if (string.IsNullOrEmpty(this.StringFormat))
                return string.Format(this.StringFormat, value);
            else
                return value?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
