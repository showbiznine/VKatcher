using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKCatcherShared.Models
{
    public class OneDriveUploadModel
    {
        public string @contentsourceUrl { get; set; }
        public string name { get; set; }
        public File file { get; set; }

        public class File
        {
        }

    }
}
