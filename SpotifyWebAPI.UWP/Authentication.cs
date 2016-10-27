using Newtonsoft.Json;
using SpotifyWebAPI.SpotifyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyWebAPI
{
    /// <summary>
    /// 
    /// </summary>
    public class Authentication : BaseModel
    {
        public static string ClientId = "b6e7b395c1604538aee605e8dc0b506c";

        public static string ClientSecret = "fe595aad83614d5bb1385e2d277727ee";

        public static string RedirectUri = "ms-app://s-1-15-2-2947634317-3937512038-3809615582-3149502856-751209071-2802032941-745678208/";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async static Task<AuthenticationToken> GetAccessToken(string code)
        {
            Dictionary<string, string> postData = new Dictionary<string, string>();
            postData.Add("grant_type", "authorization_code");
            postData.Add("code", code);
            postData.Add("redirect_uri", RedirectUri);
            postData.Add("client_id", ClientId);
            postData.Add("client_secret", ClientSecret);

            var json = await HttpHelper.Post("https://accounts.spotify.com/api/token", postData);
            var obj = JsonConvert.DeserializeObject<accesstoken>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                });

            return obj.ToPOCO();
        }
    }
}
