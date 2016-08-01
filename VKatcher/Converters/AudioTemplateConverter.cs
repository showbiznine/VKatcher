using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace VKatcher.Converters
{
    public class AudioTemplateConverter : DataTemplateSelector
    {

        public DataTemplate SongListTemplate { get; set; }
        public DataTemplate AudioListTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject obj)
        {
            if (item is VKAttachment)
            {
                //return (DataTemplate)Application.Current.Resources["SongListTemplate"];
                return SongListTemplate;
            }
            else if (item is VKAudio)
            {
                //return (DataTemplate)Application.Current.Resources["AudioListTemplate"];
                return AudioListTemplate;
            }
            else
            {
                return null;
            }
        }
    }
}
