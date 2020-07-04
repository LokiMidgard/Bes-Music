using System;
using System.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace MusicPlayer.Converters
{
    public class VisibleCollapsedOnNull : IValueConverter
    {

        public Visibility OnNullValue { get; set; }
        private Visibility OnNotNullValue => this.OnNullValue == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is null ? this.OnNullValue : this.OnNotNullValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


    public class EmptyToBool : IValueConverter
    {

        public bool OnNullValue { get; set; }
        private bool OnNotNullValue => !this.OnNullValue;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string s)
            {
                return string.IsNullOrEmpty(s) ? OnNullValue : OnNotNullValue;
            }

            if (value is int i)
                return i == 0 ? OnNullValue : OnNotNullValue;

            if (value is ICollection c)
                return c.Count == 0 ? OnNullValue : OnNotNullValue;


            return value is null ? this.OnNullValue : this.OnNotNullValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

}
