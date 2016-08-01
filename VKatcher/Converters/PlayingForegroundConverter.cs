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
    class PlayingForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value)
            {
                return (SolidColorBrush)Application.Current.Resources["SystemControlHighlightAccentBrush"];
            }
            else
                return new SolidColorBrush { Color = (Color)Application.Current.Resources["SystemBaseHighColor"] };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
