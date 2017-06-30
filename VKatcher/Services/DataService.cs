using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK;
using VK.WindowsPhone.SDK.API;
using VK.WindowsPhone.SDK.API.Model;
using VK.WindowsPhone.SDK.Util;
using VKatcher.Models;
using VKatcher.ViewModels;
using VKatcher.Views;
using VKatcherShared.Services;
using VKCatcherShared.Models;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

namespace VKatcher.Services
{
    public class DataService
    {

        #region API Calls

        #region Generic Methods
        private static async Task<string> HttpGet(string query)
        {
            var request = new HttpHelperRequest(new Uri(query), HttpMethod.Get);
            request.Headers.Add("User-Agent", Constants.userAgent);

            using (var response = await HttpHelper.Instance.SendRequestAsync(request))
            {
                if (response.Success)
                    return await response.GetTextResultAsync();
                else
                    return null;
            }
        }
        #endregion

        #region Groups
        public static async Task<ObservableCollection<VKGroup>> LoadMyGroups()
        {
            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"extended", "1" },
                {"count", "100" },
                {"access_token", await AuthenticationService.GetVKAccessToken() },
                {"v", Constants.apiVersion }
            };
            string request = Constants.host + "groups.get?" + q;

            var r = JsonConvert.DeserializeObject<VKGroupRoot>(await HttpGet(request));

            return r.response.items;
        }

        public static async Task<ObservableCollection<VKWallPost>> LoadWallPosts(long groupID, int offset, int count)
        {
            var WallPosts = new ObservableCollection<VKWallPost>();

            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", Constants.userAgent);
            var q = new QueryString()
            {
                {"owner_id", "-" + groupID },
                {"extended", "1" },
                {"offset", offset.ToString() },
                {"count", count.ToString() },
                {"access_token", await AuthenticationService.GetVKAccessToken() },
                {"v", Constants.apiVersion }
            };
            string request = Constants.host + "wall.get?" + q;

            var ReturnedObject = JsonConvert.DeserializeObject<VKWallPostRoot>(await HttpGet(request));
            var r = ReturnedObject.response.items;
            foreach (var wp in r)
            {
                var temp = wp;
                if (temp.attachments != null
                    && temp.attachments.Count > 0)
                {
                    temp = FormatPost(temp);
                }
                else if (temp.copy_history != null
                && temp.copy_history.Count > 0)
                {
                    temp = FormatPost(temp.copy_history[0]);
                }
                if (temp.attachments != null
                    && temp.attachments.Count > 0)
                {
                    WallPosts.Add(temp);
                }
            }
            await CheckOffline(WallPosts);
            return WallPosts;
        }
        #endregion

        #region Search
        public static async Task<ObservableCollection<VKGroup>> SearchGroups(string query)
        {
            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"q", query },
                {"type", "group" },
                {"access_token", await AuthenticationService.GetVKAccessToken() },
                {"v", Constants.apiVersion }
            };
            string request = Constants.host + "groups.search?" + q;

            var r = JsonConvert.DeserializeObject<VKGroupRoot>(await HttpGet(request));
            return r.response.items;
        }

        public static async Task<ObservableCollection<VKAudio>> SearchAudio(string query)
        {
            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"q", query },
                {"auto_complete", "true" },
                {"access_token", await AuthenticationService.GetVKAccessToken() },
                {"v", Constants.apiVersion }
            };
            string request = Constants.host + "audio.search?" + q;

            var ReturnedObject = JsonConvert.DeserializeObject<VKAudioRoot>(await HttpGet(request));
            var r = ReturnedObject.response.items;
            foreach (var track in r)
            {
                track.IsPlaying = CheckPlaying(track);
            }
            await CheckOffline(r);
            return r;
        }

        public static async Task<ObservableCollection<VKWallPost>> SearchWallByTag(string query, string domain, int count)
        {
            var WallPosts = new ObservableCollection<VKWallPost>();

            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"query", "#" + query },
                {"domain", domain },
                {"count", count.ToString() },
                {"access_token", await AuthenticationService.GetVKAccessToken() },
                {"v", Constants.apiVersion }
            };
            string request = Constants.host + "wall.search?" + q;

            var ReturnedObject = JsonConvert.DeserializeObject<VKWallPostRoot>(await HttpGet(request));
            var r = ReturnedObject.response.items;
            foreach (var wp in r)
            {
                var temp = wp;
                if (temp.attachments != null
                    && temp.attachments.Count > 0)
                {
                    temp = FormatPost(temp);
                }
                else if (temp.copy_history != null
                && temp.copy_history.Count > 0)
                {
                    temp = FormatPost(temp.copy_history[0]);
                }
                if (temp.attachments != null
                    && temp.attachments.Count > 0)
                {
                    WallPosts.Add(temp);
                }
            }
            return WallPosts;
        }
        #endregion

        #region My Audio
        public static async Task<ObservableCollection<VKAudio>> LoadMyAudio()
        {
            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"count", "100" },
                {"access_token", await AuthenticationService.GetVKAccessToken() },
                {"v", Constants.apiVersion }
            };
            string request = Constants.host + "audio.get?" + q;

            var ReturnedObject = JsonConvert.DeserializeObject<VKAudioRoot>(await HttpGet(request));
            var r = ReturnedObject.response.items;
            foreach (var item in r)
            {
                item.IsPlaying = CheckPlaying(item);
            }
            await CheckOffline(r);
            return r;
        }

        public static async Task<VKAudio> SearchAudioById(string id, string ownerid)
        {
            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"audios", ownerid + "_" + id },
                {"access_token", await AuthenticationService.GetVKAccessToken() },
                {"v", Constants.apiVersion }
            };
            string request = Constants.host + "audio.getById?" + q;

            var r = JsonConvert.DeserializeObject<VKAudioByIdRoot>(await HttpGet(request));
            //await CheckOffline(r);
            return r.response[0];
        }

        #endregion

        #region Radio

        public static async Task<VKRadio> GetMyRadio()
        {
            HttpClient client = new HttpClient();
            var q = new QueryString
            {
                {"access_token", await AuthenticationService.GetVKAccessToken() },
                {"v", Constants.apiVersion }
            };
            var uri = new Uri(Constants.host + "audio.getRecommendations?" + q);

            var res = await client.GetAsync(uri);
            var s = await res.Content.ReadAsStringAsync();

            var r = JsonConvert.DeserializeObject<VKRadio>(s);
            return r;
        }

        public static async Task<VKRadio> GetRadioByTrack(VKAudio Track)
        {
            HttpClient client = new HttpClient();
            var q = new QueryString
            {
                {"target_audio", Track.owner_id + "_" + Track.id },
                {"access_token", await AuthenticationService.GetVKAccessToken() },
                {"v", Constants.apiVersion }
            };

            var uri = new Uri(Constants.host + "audio.getRecommendations?" + q);

            var res = await client.GetAsync(uri);
            var s = await res.Content.ReadAsStringAsync();

            var r = JsonConvert.DeserializeObject<VKRadio>(s);
            return null;
        }
        #endregion

        #endregion

        #region Formatting
        private static VKWallPost FormatPost(VKWallPost post)
        {

            #region Tags
            post.text = FormatPostText(post.text);
            #endregion

            for (int i = post.attachments.Count - 1; i > -1; i--)
            {
                var attachment = post.attachments[i];
                switch (attachment.type)
                {
                    case "audio":
                        attachment.audio.IsPlaying = CheckPlaying(attachment.audio);
                        break;
                    #region Photo
                    case "photo":
                        try
                        {
                            post.post_image_aspect_ratio = attachment.photo.width / attachment.photo.height;
                        }
                        catch (Exception)
                        {
                            post.post_image_aspect_ratio = 1;
                        }
                        if (attachment.photo.photo_2560 != null)
                        {
                            post.post_image = attachment.photo.photo_2560;
                        }
                        else if (attachment.photo.photo_1280 != null)
                        {
                            post.post_image = attachment.photo.photo_1280;
                        }
                        else if (attachment.photo.photo_807 != null)
                        {
                            post.post_image = attachment.photo.photo_807;
                        }
                        else if (attachment.photo.photo_604 != null)
                        {
                            post.post_image = attachment.photo.photo_604;
                        }
                        else if (attachment.photo.photo_130 != null)
                        {
                            post.post_image = attachment.photo.photo_130;
                        }
                        else if (attachment.photo.photo_75 != null)
                        {
                            post.post_image = attachment.photo.photo_75;
                        }
                        if (!string.IsNullOrWhiteSpace(attachment.photo.text) &&
                            string.IsNullOrWhiteSpace(post.text))
                        {
                            post.text = FormatPostText(attachment.photo.text);
                        }
                        post.attachments.RemoveAt(i);
                        break;
                    default:
                        post.attachments.RemoveAt(i);
                        //Or else remove the attachment
                        break;
                        #endregion
                }
            }
            return post;
        }

        private static string FormatPostText(string text)
        {
            /// Tweak this to escape _underlines_ making things italic

            #region Line Breaks
            //New line code needs a double space in Markdown
            text = text.Replace("\n", " \n");
            #endregion

            #region Tags
            Regex r = new Regex(@"#(\w*[0-9a-zA-Z]+@?\w*[0-9a-zA-Z])");
            foreach (Match m in r.Matches(text))
            {
                var newTag = "[" + m.Value + "](" + m.Value + ")";
                text = text.Replace(m.Value, newTag);
            }
            //Escape underscores
            text = text.Replace("_", @"\_");
            #endregion
            return text;
        } 
        #endregion

        #region Status Checks
        private static bool CheckPlaying(VKAudio track)
        {
            //if (App.ViewModelLocator.Main._currentTrack != null)
            //    return App.ViewModelLocator.Main._currentTrack.id == track.id;

            return false;
        }

        private static async Task CheckOffline(ObservableCollection<VKAudio> tracks)
        {
            var downloads = await FileService.GetDownloads();

            foreach (var track in tracks)
            {
                var res = downloads.FirstOrDefault(x => x.id == track.id);
                if (res != null)
                    track.IsOffline = true;
                else
                    track.IsOffline = false;
            }
        }

        private static async Task CheckOffline(ObservableCollection<VKWallPost> posts)
        {
            var downloads = await FileService.GetDownloads();

            foreach (var post in posts)
            {
                foreach (var track in post.attachments)
                {
                    var res = downloads.FirstOrDefault(x => x.id == track.audio.id);
                    if (res != null)
                        track.audio.IsOffline = true;
                    else
                        track.audio.IsOffline = false;
                }
            }
        } 
        #endregion
    }
}
