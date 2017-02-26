using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;

namespace VKCatcherShared.Models
{
    public class VKRadio
    {

        public VKRadioResponse response { get; set; }

        public class VKRadioResponse
        {
            public int count { get; set; }
            public ObservableCollection<VKAudio> items { get; set; }
        }
    }
}
