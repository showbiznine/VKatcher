using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;

namespace VKatcherShared.Models
{
    public class Response
    {
        public ObservableCollection<VKWallPost> wall { get; set; }
        public List<object> profiles { get; set; }
        public List<VKGroup> groups { get; set; }
    }

    public class DIYWallPost
    {
        public Response response { get; set; }
    }
}
