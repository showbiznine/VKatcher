using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace VKatcherShared.Models
{
    public class DownloadedTrack
    {
        public string file_path { get; set; }

        public string title { get; set; }
        public string artist { get; set; }
        public int duration { get; set; }

        public Image album_art { get; set; }
    }
}
