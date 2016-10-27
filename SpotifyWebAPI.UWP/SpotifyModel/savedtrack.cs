using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpotifyWebAPI.SpotifyModel
{
    [JsonObject]
    class savedtrack
    {
        public string added_at { get; set; }
        public track track { get; set; }
    }
}
