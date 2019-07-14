using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace MusicPlayer.Converters
{
    public class VisibleCollapsedOnEmpty : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IEnumerable<object> enumerable)
            {
                return enumerable.Any() ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (value is int i)
            {
                return i > 0 ? Visibility.Visible : Visibility.Collapsed;

            }
            else if (value is long l)
            {
                return l > 0 ? Visibility.Visible : Visibility.Collapsed;

            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
