using Microsoft.QueryStringDotNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
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

namespace VKatcher.Services
{
    public class DataService
    {
        private const string _host = "https://api.vk.com/method/";
        private const string _authHost = "https://oauth.vk.com/";
        private const string _apiVersion = "5.63";

        #region API Calls

        #region Groups
        public static async Task<ObservableCollection<VKGroup>> LoadMyGroups()
        {
            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"extended", "1" },
                {"count", "100" },
                {"access_token", await AuthenticationService.GetVKAccessToken() },
                {"v", _apiVersion }
            };
            string request = _host + "groups.get?" + q;

            var res = await http.GetAsync(request);
            var json = await res.Content.ReadAsStringAsync();
            if (json.Contains("error_code"))
            {
                await AuthenticationService.VKLogin();
                return null;
            }
            var r = JsonConvert.DeserializeObject<VKGroupRoot>(json);
            return r.response.items;
        }

        public static async Task<ObservableCollection<VKWallPost>> LoadWallPosts(long groupID, int offset, int count, bool foreground)
        {
            try
            {
                var WallPosts = new ObservableCollection<VKWallPost>();

                HttpClient http = new HttpClient();
                var q = new QueryString()
                {
                    {"owner_id", "-" + groupID },
                    {"extended", "1" },
                    {"offset", offset.ToString() },
                    {"count", count.ToString() },
                    {"access_token", await AuthenticationService.GetVKAccessToken() },
                    {"v", _apiVersion }
                };
                string request = _host + "wall.get?" + q;

                var res = await http.GetAsync(request);
                var json = await res.Content.ReadAsStringAsync();

                var ReturnedObject = JsonConvert.DeserializeObject<VKWallPostRoot>(json);
                var r = ReturnedObject.response.items;
                foreach (var wp in r)
                {
                    var temp = wp;
                    if (temp.attachments != null
                        && temp.attachments.Count > 0)
                    {
                        temp = FormatPost(temp, foreground);
                    }
                    else if (temp.copy_history != null
                    && temp.copy_history.Count > 0)
                    {
                        temp = FormatPost(temp.copy_history[0], foreground);
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
            catch (Exception ex)
            {

                throw;
            }
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
                {"v", _apiVersion }
            };
            string request = _host + "groups.search?" + q;

            var res = await http.GetAsync(request);
            var json = await res.Content.ReadAsStringAsync();

            var r = JsonConvert.DeserializeObject<VKGroupRoot>(json);
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
                {"v", _apiVersion }
            };
            string request = _host + "audio.search?" + q;

            var res = await http.GetAsync(request);
            var json = await res.Content.ReadAsStringAsync();

            var ReturnedObject = JsonConvert.DeserializeObject<VKAudioRoot>(json);
            var r = ReturnedObject.response.items;
            foreach (var track in r)
            {
                track.IsPlaying = CheckPlaying(track);
            }
            await CheckOffline(r);
            return r;
        }

        public static async Task<ObservableCollection<VKWallPost>> SearchWallByTag(string query, string domain, int count, bool foreground)
        {
            var WallPosts = new ObservableCollection<VKWallPost>();

            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"query", "#" + query },
                {"domain", domain },
                {"count", count.ToString() },
                {"access_token", await AuthenticationService.GetVKAccessToken() },
                {"v", _apiVersion }
            };
            string request = _host + "wall.search?" + q;

            var res = await http.GetAsync(request);
            var json = await res.Content.ReadAsStringAsync();

            var ReturnedObject = JsonConvert.DeserializeObject<VKWallPostRoot>(json);
            var r = ReturnedObject.response.items;
            foreach (var wp in r)
            {
                var temp = wp;
                if (temp.attachments != null
                    && temp.attachments.Count > 0)
                {
                    temp = FormatPost(temp, foreground);
                }
                else if (temp.copy_history != null
                && temp.copy_history.Count > 0)
                {
                    temp = FormatPost(temp.copy_history[0], foreground);
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
                {"v", _apiVersion }
            };
            string request = _host + "audio.get?" + q;

            var res = await http.GetAsync(request);
            var json = await res.Content.ReadAsStringAsync();

            var ReturnedObject = JsonConvert.DeserializeObject<VKAudioRoot>(json);
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
                {"v", _apiVersion }
            };
            string request = _host + "audio.getById?" + q;

            var res = await http.GetAsync(request);
            var json = await res.Content.ReadAsStringAsync();

            var r = JsonConvert.DeserializeObject<VKAudioByIdRoot>(json);
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
                {"v", _apiVersion }
            };
            var uri = new Uri(_host + "audio.getRecommendations?" + q);

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
                {"v", _apiVersion }
            };

            var uri = new Uri(_host + "audio.getRecommendations?" + q);

            var res = await client.GetAsync(uri);
            var s = await res.Content.ReadAsStringAsync();

            var r = JsonConvert.DeserializeObject<VKRadio>(s);
            return null;
        }
        #endregion

        #endregion

        #region Formatting
        private static VKWallPost FormatPost(VKWallPost post, bool foreground)
        {
            try
            {
                string postImage = null;
                #region Tags
                post.text = FormatPostText(post.text);
                #endregion

                for (int i = 0; i < post.attachments.Count; i++)
                {
                    var attachment = post.attachments[i];
                    switch (attachment.type)
                    {
                        case "audio":
                            if (foreground)
                                attachment.audio.IsPlaying = CheckPlaying(attachment.audio);
                            //attachment.audio.photo_url = postImage;
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

                            if (foreground)
                            {
                                if (attachment.photo.photo_2560 != null)
                                    post.post_image = postImage = attachment.photo.photo_2560;
                                else if (attachment.photo.photo_1280 != null && postImage == null)
                                    post.post_image = postImage = attachment.photo.photo_1280;
                                else if (attachment.photo.photo_807 != null && postImage == null)
                                    post.post_image = postImage = attachment.photo.photo_807;
                            }

                            //else if (attachment.photo.photo_604 != null && postImage == null)
                            //{
                            //    post.post_image = postImage = attachment.photo.photo_604;
                            //}
                            //else if (attachment.photo.photo_130 != null && postImage == null)
                            //{
                            //    post.post_image = postImage = attachment.photo.photo_130;
                            //}
                            //else if (attachment.photo.photo_75 != null && postImage == null)
                            //{
                            //    post.post_image = postImage = attachment.photo.photo_75;
                            //}

                            if (!string.IsNullOrWhiteSpace(attachment.photo.text) &&
                                string.IsNullOrWhiteSpace(post.text))
                            {
                                post.text = FormatPostText(attachment.photo.text);
                            }
                            post.attachments.Remove(attachment);
                            i--;
                            break;
                        default:
                            post.attachments.Remove(attachment);
                            i--;
                            //Or else remove the attachment
                            break;
                            #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
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
            if (App.ViewModelLocator.Main._currentTrack != null)
                return App.ViewModelLocator.Main._currentTrack.id == track.id;

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
