using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKatcher.Models
{
    public class VKToken
    {

        public string access_token { get; set; }
        public int expires_in { get; set; }
        public int user_id { get; set; }

    }
}
