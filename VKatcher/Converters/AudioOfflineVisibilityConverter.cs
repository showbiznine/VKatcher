using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace VKatcher.Converters
{
    class AudioOfflineVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool invert = false;
            if ((string)parameter == "true")
            {
                invert = true;
            }            

            if (value is VKAttachment)
            {
                var att = value as VKAttachment;
                if (att.audio.IsOffline)
                {
                    return invert ? Visibility.Collapsed : Visibility.Visible;
                }
                else return invert ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (value is VKAudio)
            {
                var audio = value as VKAudio;
                if (audio.IsOffline)
                {
                    return invert ? Visibility.Collapsed : Visibility.Visible;
                }
                else return invert ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
