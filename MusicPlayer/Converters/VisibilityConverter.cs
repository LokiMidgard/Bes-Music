using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace MusicPlayer.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public Visibility OnTrue { get; set; } = Visibility.Visible;
        public Visibility OnFalse => this.OnTrue == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return b ? this.OnTrue : this.OnFalse;

            throw new NotImplementedException();

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


    public class VisibilityOnEqualsConverter : IValueConverter
    {
        public Visibility OnEquals { get; set; } = Visibility.Visible;
        public Visibility OnNotEquals => this.OnEquals == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value?.GetType().GetTypeInfo().IsEnum?? false)
            {
                value = (int)value; // enums defind in xaml are for whatever rea
            }
            if (Equals(value, parameter))
                return this.OnEquals;
            return this.OnNotEquals;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

}
