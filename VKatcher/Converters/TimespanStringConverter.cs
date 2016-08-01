using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace VKatcher.Converters
{
    class TimespanStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var ts = value;
            var seconds = System.Convert.ToInt32(ts);
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            if (seconds > 3600)
            {
                return string.Format("{0}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            }
            return string.Format("{0}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
