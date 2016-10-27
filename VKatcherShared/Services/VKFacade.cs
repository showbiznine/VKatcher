using Microsoft.QueryStringDotNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK;
using VK.WindowsPhone.SDK.API;
using VK.WindowsPhone.SDK.API.Model;
using VK.WindowsPhone.SDK.Util;
using VKatcherShared.Models;

namespace VKatcherShared.Services
{
    public class VKFacade
    {
        private const string _host = "https://api.vk.com/method/";
        private const string _apiVersion = "5.53";

        public static ObservableCollection<VKWallPost> LoadWallPosts(long groupID, int? postCount, int offset)
        {
            var WallPosts = new ObservableCollection<VKWallPost>();

            var param = new VKRequestParameters("wall.get",
                "owner_id", "-" + groupID.ToString(),
                "extended", "1",
                "offset", offset.ToString(),
                "count", "30");

            VKRequest.Dispatch<VKList<VKWallPost>>(
               param , async (res) =>
            {
                await VKExecute.ExecuteOnUIThread(() =>
                {
                    try
                    {
                        if (res.ResultCode == VKResultCode.Succeeded)
                        {
                            var payload = res.Data.items;
                            foreach (var post in payload)
                            {
                                var temp = post;
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
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                });
            });

            return WallPosts;
        }

        public static ObservableCollection<VKAttachment> LoadAudioPosts()
        {
            var MyAudio = new ObservableCollection<VKAttachment>();

            VKRequest.Dispatch<VKList<VKAudio>>(
                new VKRequestParameters("audio.get",
                "count", "100"), async (res) =>
                {
                    await VKExecute.ExecuteOnUIThread(() =>
                    {
                        try
                        {
                            if (res.ResultCode == VKResultCode.Succeeded)
                            {
                                var payload = res.Data.items;
                                foreach (var track in payload)
                                {
                                    var item = new VKAttachment
                                    {
                                        audio = track
                                    };
                                    MyAudio.Add(item);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    });
                });

            return MyAudio;
        }

        private static VKWallPost FormatPost(VKWallPost post)
        {
            for (int i = post.attachments.Count - 1; i > -1; i--)
            {
                var attachment = post.attachments[i];
                switch (attachment.type)
                {
                    case "audio":
                        //attachment.audio.photo_url = post.post_image;
                        break;
                    case "photo":
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
                        post.attachments.RemoveAt(i);
                        break;
                    default:
                        post.attachments.RemoveAt(i);
                        //Or else remove the attachment
                        break;
                }
            }
            foreach (var audio in post.attachments)
            {
                audio.audio.photo_url = post.post_image;
                //Assign image
            }
            return post;
        }

        public static async Task<ObservableCollection<VKWallPost>> LoadWallPostsDIY(long groupID, int? count, int offset)
        {
            var WallPosts = new ObservableCollection<VKWallPost>();
            var parameters = new QueryString
            {
                { "owner_id", "-" + groupID.ToString() },
                { "extended", "1" },
                { "offset", offset.ToString() },
                { "count", count.ToString() }
            };
            var http = new HttpClient();
            var tok = VKSDK.GetAccessToken();
            string str = string.Format("https://api.vk.com/method/{0}?{1}&access_token={2}",
                "wall.get",
                parameters,
                tok.AccessToken);

            var result = await http.GetAsync(str);

            var json = await result.Content.ReadAsStringAsync();
            json = json.Replace("aid", "id");
            var temp = json.Split(new char[] {',', '['}, 3);

            string fix = temp[0] + "[" + temp[2];
            var res = JsonConvert.DeserializeObject<DIYWallPost>(fix);
            foreach (var post in res.response.wall)
            {
                var temp3 = post;
                if (temp3.attachments != null
                    && temp3.attachments.Count > 0)
                {
                    temp3 = FormatPost(temp3);
                }
                else if (temp3.copy_history != null
                && temp3.copy_history.Count > 0)
                {
                    temp3 = FormatPost(temp3.copy_history[0]);
                }
                if (temp3.attachments != null
                    && temp3.attachments.Count > 0)
                
                    WallPosts.Add(temp3);
                }
            return WallPosts;
        }

        public static async Task<ObservableCollection<VKAudio>> SearchAudio(string query)
        {
            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"q", query },
                {"auto_complete", "true" },
                {"access_token", VKSDK.GetAccessToken().AccessToken },
                {"v", _apiVersion }
            };
            string request = _host + "audio.search?" + q;

            var res = await http.GetAsync(request);
            var json = await res.Content.ReadAsStringAsync();

            var ReturnedObject = JsonConvert.DeserializeObject<VKAudioRoot>(json);
            var r = ReturnedObject.response.items;
            //foreach (var track in r)
            //{
            //    track.IsPlaying = CheckPlaying(track);
            //}
            //await CheckOffline(r);
            return r;
        }
    }
}
