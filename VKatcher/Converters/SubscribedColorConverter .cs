using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace VKatcher.Converters
{
    class SubscribedColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool b = (bool)value;
            if (b)
            {
                return new SolidColorBrush { Color = (Color)Application.Current.Resources["SystemAccentColor"] };
            }
            else
            {
                return new SolidColorBrush { Color = (Color)Application.Current.Resources["SystemBaseHighColor"] };
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
