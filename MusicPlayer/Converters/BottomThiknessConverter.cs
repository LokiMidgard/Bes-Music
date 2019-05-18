using MusicPlayer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace MusicPlayer.Converters
{
    class ThiknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null)
                return null;

            if (!(value is double d))
                throw new NotImplementedException();



            var configuration = parameter as ThiknessType? ?? default;

            var left = configuration.HasFlag(ThiknessType.Left) ? d : 0;
            var top = configuration.HasFlag(ThiknessType.Top) ? d : 0;
            var right = configuration.HasFlag(ThiknessType.Right) ? d : 0;
            var bottom = configuration.HasFlag(ThiknessType.Bottom) ? d : 0;


            return new Thickness(left, top, right, bottom);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }




        public static ThiknessType GetConstBindThickness(DependencyObject obj)
        {
            return (ThiknessType)obj.GetValue(ConstBindThicknessProperty);
        }

        public static void SetConstBindThickness(DependencyObject obj, ThiknessType value)
        {
            obj.SetValue(ConstBindThicknessProperty, value);
        }

        // Using a DependencyProperty as the backing store for ConstBindThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConstBindThicknessProperty =
            DependencyProperty.RegisterAttached("ConstBindThickness", typeof(ThiknessType), typeof(ThiknessConverter), new PropertyMetadata(ThiknessType.None, TypeChanged));

        private static void TypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            var toConfigure = (ThiknessType)(e.NewValue as int? ?? 0);


            var margin = FrameworkElement.MarginProperty;
            var padding = Control.PaddingProperty;

            if (d is FrameworkElement)
                if (toConfigure.HasFlag(ThiknessType.Margin))
                {
                    BindingOperations.SetBinding(
                        d,
                        margin,
                        new Binding
                        {
                            Path = new PropertyPath(nameof(ConstantsHelper.PlayListHeight)),
                            Source = new ConstantsHelper(),
                            Converter = new ThiknessConverter(),
                            ConverterParameter = toConfigure
                        });

                }
                else
                {
                    d.ClearValue(margin);
                }

            if (d is Control)
                if (toConfigure.HasFlag(ThiknessType.Padding))
                {
                    BindingOperations.SetBinding(
                        d,
                        padding,
                        new Binding
                        {
                            Path = new PropertyPath(nameof(ConstantsHelper.PlayListHeight)),
                            Source = new ConstantsHelper(),
                            Converter = new ThiknessConverter(),
                            ConverterParameter = toConfigure
                        });

                }
                else
                {
                    d.ClearValue(padding);
                }

        }
    }

    [Flags]
    public enum ThiknessType 
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8,
        Margin = 16,
        Padding = 32
    }
}
