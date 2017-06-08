using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKatcher.Models
{
    public class VKRefreshToken
    {

        public Response response { get; set; }

        public class Response
        {
            public string token { get; set; }
        }

    }
}
