using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MusicPlayer.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        public int Precision { get; set; } = 2;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var size = System.Convert.ToDecimal(value);

            string[] magnitude = new string[] {
                "Byte",
                "KB",
                "MB",
                "GB",
                "TB",
                "PB"
            };

            var index = 0;
            while (size >= 1024 && index < magnitude.Length - 1)
            {
                size /= 1024;
                index++;
            }

            return string.Format("{0:N" + this.Precision + "}{1}", size, magnitude[index]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
