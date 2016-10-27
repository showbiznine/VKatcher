using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpotifyWebAPI.SpotifyModel
{
    [JsonObject]
    internal class tracksearchresult
    {
        public page<track> tracks { get; set; }
    }
}
