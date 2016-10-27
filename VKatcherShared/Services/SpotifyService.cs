using Microsoft.QueryStringDotNET;
using Newtonsoft.Json;
using SpotifyWebAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using VKatcherShared.Services;
using VKCatcherShared.Models.Spotify;
using Windows.Security.Authentication.Web;

namespace VKCatcherShared.Services
{
    public class SpotifyService
    {
        public static string AccountsRoot = "https://accounts.spotify.com/";
        public static string APIRoot = "https://api.spotify.com/v1/";
        public static string ClientID = "b6e7b395c1604538aee605e8dc0b506c";
        public static string ClientSecret = "fe595aad83614d5bb1385e2d277727ee";
        public static AuthenticationToken AuthToken { get; set; }

        public static async void Authenticate()
        {
            //var q = await SpotifyWebAPI.Authentication.GetAccessToken("a");
            await GetAuthCode();
        }

        public static async Task<bool> GetAuthCode()
        {
            var redirect = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString();
            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                { "client_id", ClientID},
                { "response_type", "code"},
                { "redirect_uri", redirect },
                { "scope", "playlist-read-private playlist-read-collaborative" },
                { "show_dialog", "false" }
            };

            string request = AccountsRoot + "authorize/?" + q;

            WebAuthenticationResult result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(request), new Uri(redirect));

            if (result.ResponseStatus == WebAuthenticationStatus.Success)
            {
                var split = result.ResponseData.ToString().Split('=');
                AuthToken = await Authentication.GetAccessToken(split[1]);
                Debug.WriteLine(AuthToken.AccessToken);
                return true;
            }
            return false;
        }

        public static async Task<List<Playlist>> GetUserPlaylists()
        {
            var user = await User.GetCurrentUserProfile(AuthToken);
            var playlists = await user.GetPlaylists(AuthToken);
            return playlists.Items;
            //foreach (var playlist in playlists.Items)
            //{
            //    Debug.WriteLine("Checking the playlist \"" + playlist.Name + "\"");
            //    var tracks = await playlist.GetPlaylistTracks(AuthToken);
            //    foreach (var plTrack in tracks.Items)
            //    {
            //        var match = await MatchTrack(plTrack.Track);
            //        await Task.Delay(1000); //Due to API call limits
            //    }
            //}
        }

        public static async Task<VKAudio> MatchTrack(Track track)
        {
            Debug.WriteLine("Searching for " + track.Artists[0].Name + " - " + track.Name);
            var duration = Convert.ToInt64(track.Duration / 1000);

            var matches = await VKFacade.SearchAudio(track.Name + " " + track.Artists[0].Name);
            Debug.WriteLine("Found " + matches.Count + " possible matches");

            VKAudio closestMatch = new VKAudio();            

            if (matches.Count > 0)
            {
                closestMatch = matches.FirstOrDefault(x => x.duration == duration);
                if (closestMatch == null)
                {
                    closestMatch = matches.Aggregate((x, y) => Math.Abs(x.duration - duration) < Math.Abs(y.duration - duration) ? x : y);
                }
                return closestMatch;
            }
            else
            {
                return null;
            }
        }
    }
}
