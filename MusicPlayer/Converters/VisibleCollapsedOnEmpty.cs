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
        public Visibility OnNullValue { get; set; }
        private Visibility OnNotNullValue => this.OnNullValue == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;


        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IEnumerable<object> enumerable)
            {
                return enumerable.Any() ? this.OnNotNullValue : this.OnNullValue;
            }
            else if (value is int i)
            {
                return i > 0 ? this.OnNotNullValue : this.OnNullValue;

            }
            else if (value is long l)
            {
                return l > 0 ? this.OnNotNullValue : this.OnNullValue;

            }

            return value is null ? this.OnNullValue : this.OnNotNullValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

}
