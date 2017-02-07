﻿using Microsoft.QueryStringDotNET;
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
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace VKatcher.Services
{
    public class DataService
    {
        private const string _host = "https://api.vk.com/method/";
        private const string _authHost = "https://oauth.vk.com/";
        //private const string _apiVersion = "5.53";
        private const string _apiVersion = "5.62";

        #region VKSDK
        //public static async Task<ObservableCollection<VKGroup>> LoadMyGroups()
        //{
        //    var MyGroups = new ObservableCollection<VKGroup>();
        //    await Task.Run(() =>
        //    {
        //        VKRequest.Dispatch<VKList<VKGroup>>(new VKRequestParameters("groups.get",
        //        "extended", "1"),
        //        async (res) =>
        //        {
        //            await VKExecute.ExecuteOnUIThread(() =>
        //            {
        //                if (res.ResultCode == VKResultCode.Succeeded)
        //                {
        //                    var groupList = res.Data.items;
        //                    foreach (var group in groupList)
        //                    {
        //                        MyGroups.Add(group);
        //                    }
        //                }
        //            });
        //        });
        //    });
        //    return MyGroups;
        //}

        //public static async Task<ObservableCollection<VKGroup>> SearchGroups(string query)
        //{
        //    var MyGroups = new ObservableCollection<VKGroup>();
        //    await Task.Run(() =>
        //    {
        //        VKRequest.Dispatch<VKList<VKGroup>>(new VKRequestParameters("groups.search",
        //            "q", query,
        //            "type", "group"),
        //            async (res) =>
        //            {
        //                await VKExecute.ExecuteOnUIThread(() =>
        //                {
        //                    if (res.ResultCode == VKResultCode.Succeeded)
        //                    {
        //                        var groupList = res.Data.items;
        //                        foreach (var group in groupList)
        //                        {
        //                            MyGroups.Add(group);
        //                        }
        //                    }
        //                });
        //            });
        //    });
        //    return MyGroups;
        //}

        //public static async Task<ObservableCollection<VKAudio>> SearchAudio(string query)
        //{
        //    var MyAudio = new ObservableCollection<VKAudio>();
        //    var vm = ((Window.Current.Content as Frame).Content as MainPage).DataContext as MainViewModel;

        //    await Task.Run(() =>
        //    {
        //        VKRequest.Dispatch<VKList<VKAudio>>(new VKRequestParameters("audio.search",
        //            "q", query,
        //            "auto_complete", "true"),
        //            async (res) =>
        //            {
        //                await VKExecute.ExecuteOnUIThread(() =>
        //                {
        //                    if (res.ResultCode == VKResultCode.Succeeded)
        //                    {
        //                        var tracks = res.Data.items;
        //                        foreach (var track in tracks)
        //                        {
        //                            var t = vm._currentTrack;
        //                            if (t != null &&
        //                                track.id == t.id)
        //                            {
        //                                track.IsPlaying = true;
        //                                App.ViewModelLocator.Feed._selectedTrack = track;
        //                            }
        //                            else
        //                                track.IsPlaying = false;
        //                            MyAudio.Add(track);
        //                        }
        //                    }
        //                });
        //            });
        //    });
        //    return MyAudio;
        //}

        //public static async Task<ObservableCollection<VKWallPost>> LoadWallPosts(long groupID, int offset, int count)
        //{
        //    var WallPosts = new ObservableCollection<VKWallPost>();

        //    var param = new VKRequestParameters("wall.get",
        //        "owner_id", "-" + groupID.ToString(),
        //        "extended", "1",
        //        "offset", offset.ToString(),
        //        "count", count.ToString());
        //    VKRequest.Dispatch<VKList<VKWallPost>>(
        //       param, async (res) =>
        //       {
        //           await VKExecute.ExecuteOnUIThread(async () =>
        //           {
        //               try
        //               {
        //                   if (res.ResultCode == VKResultCode.Succeeded)
        //                   {
        //                       var payload = res.Data.items;
        //                       foreach (var post in payload)
        //                       {
        //                           var temp = post;
        //                           if (temp.attachments != null
        //                               && temp.attachments.Count > 0)
        //                           {
        //                               temp = await FormatPost(temp);
        //                           }
        //                           else if (temp.copy_history != null
        //                           && temp.copy_history.Count > 0)
        //                           {
        //                               temp = await FormatPost(temp.copy_history[0]);
        //                           }
        //                           if (temp.attachments != null
        //                               && temp.attachments.Count > 0)
        //                           {
        //                               WallPosts.Add(temp);
        //                           }
        //                       }
        //                   }
        //               }
        //               catch (Exception ex)
        //               {
        //                   Debug.WriteLine(ex.Message);
        //               }
        //           });
        //       });

        //    return WallPosts;
        //}

        //public static async Task<ObservableCollection<VKAudio>> LoadMyAudio()
        //{
        //    var dls = await FileService.GetDownloads();
        //    var lst = dls.ToList();
        //    var myAudio = new ObservableCollection<VKAudio>();
        //    var vm = ((Window.Current.Content as Frame).Content as MainPage).DataContext as MainViewModel;
        //    await Task.Run(() =>
        //    {
        //        VKRequest.Dispatch<VKList<VKAudio>>(
        //            new VKRequestParameters("audio.get",
        //            "count", "100"), async (res) =>
        //            {
        //                await VKExecute.ExecuteOnUIThread(() =>
        //                {
        //                    try
        //                    {
        //                        if (res.ResultCode == VKResultCode.Succeeded)
        //                        {
        //                            var payload = res.Data.items;
        //                            foreach (var track in payload)
        //                            {
        //                                var item = track;
        //                                var tr = lst.Find(x => x.id == item.id);
        //                                if (tr != null)
        //                                {
        //                                    item = tr;
        //                                }
        //                                item.IsSaved = true;
        //                                var t = vm._currentTrack;
        //                                if (t != null &&
        //                                    item.id == t.id)
        //                                {
        //                                    item.IsPlaying = true;
        //                                    App.ViewModelLocator.Feed._selectedTrack = item;
        //                                }
        //                                else
        //                                    item.IsPlaying = false;
        //                                myAudio.Add(item);
        //                            }
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Debug.WriteLine(ex.Message);
        //                    }
        //                });
        //            });
        //    });
        //    return myAudio;
        //}

        //private static async Task<VKWallPost> FormatPost(VKWallPost post)
        //{
        //    var dls = await FileService.GetDownloads();
        //    var lst = dls.ToList();

        //    var vm = ((Window.Current.Content as Frame).Content as MainPage).DataContext as MainViewModel;
        //    for (int i = post.attachments.Count - 1; i > -1; i--)
        //    {
        //        var attachment = post.attachments[i];
        //        switch (attachment.type)
        //        {
        //            case "audio":
        //                break;
        //            #region Photo
        //            case "photo":
        //                if (attachment.photo.photo_2560 != null)
        //                {
        //                    post.post_image = attachment.photo.photo_2560;
        //                }
        //                else if (attachment.photo.photo_1280 != null)
        //                {
        //                    post.post_image = attachment.photo.photo_1280;
        //                }
        //                else if (attachment.photo.photo_807 != null)
        //                {
        //                    post.post_image = attachment.photo.photo_807;
        //                }
        //                else if (attachment.photo.photo_604 != null)
        //                {
        //                    post.post_image = attachment.photo.photo_604;
        //                }
        //                else if (attachment.photo.photo_130 != null)
        //                {
        //                    post.post_image = attachment.photo.photo_130;
        //                }
        //                else if (attachment.photo.photo_75 != null)
        //                {
        //                    post.post_image = attachment.photo.photo_75;
        //                }
        //                post.attachments.RemoveAt(i);
        //                break;
        //            default:
        //                post.attachments.RemoveAt(i);
        //                //Or else remove the attachment
        //                break;
        //                #endregion
        //        }
        //    }
        //    foreach (var audio in post.attachments)
        //    {
        //        audio.audio.photo_url = post.post_image;
        //        #region Check if downloaded
        //        var item = lst.Find(x => x.id == audio.audio.id);
        //        if (item != null)
        //        {
        //            audio.audio = item;
        //        }
        //        #endregion
        //        #region Check if playing
        //        var playingTrack = vm._currentTrack;
        //        if (playingTrack != null &&
        //            audio.audio.id == playingTrack.id)
        //        {
        //            audio.audio.IsPlaying = true;
        //            App.ViewModelLocator.Feed._selectedTrack = audio.audio;
        //        }
        //        else
        //            audio.audio.IsPlaying = false;
        //        #endregion
        //    }
        //    return post;
        //}
        #endregion

        #region Custom

        public static async Task<string> GetToken(string Username, string Password)
        {
            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"username", "showbiznine@hotmail.com" },
                {"password", "hgssucks1" },
                {"grant_type", "password" },
                {"scope", "271390" },
                {"client_id", "3697615" },
                {"client_secret", "AlVXZFMUqyrnABp8ncuU" },
            };
            string request = _authHost + "token?" + q;

            var res = await http.GetAsync(request);
            var json = await res.Content.ReadAsStringAsync();

            var r = JsonConvert.DeserializeObject<VKToken>(json);
            VKSDK.SetAccessToken(new VKAccessToken
            {
                AccessToken = r.access_token,
                UserId = r.user_id.ToString(),
                ExpiresIn = 0
            }, false);
             var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["token"] = r.access_token;
            return r.access_token;
        }

        public static async Task<ObservableCollection<VKGroup>> LoadMyGroups()
        {
            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"extended", "1" },
                {"count", "100" },
                {"access_token", await GetAccessToken() },
                {"v", _apiVersion }
            };
            string request = _host + "groups.get?" + q;

            var res = await http.GetAsync(request);
            var json = await res.Content.ReadAsStringAsync();

            var r = JsonConvert.DeserializeObject<VKGroupRoot>(json);
            return r.response.items;
        }

        public static async Task<ObservableCollection<VKGroup>> SearchGroups(string query)
        {
            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"q", query },
                {"type", "group" },
                {"access_token", await GetAccessToken() },
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
                {"access_token", await GetAccessToken() },
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

        public static async Task<ObservableCollection<VKWallPost>> SearchWallByTag(string query, string domain)
        {
            var WallPosts = new ObservableCollection<VKWallPost>();

            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"query", "#" + query },
                {"domain", domain },
                {"count", "30" },
                {"access_token", await GetAccessToken() },
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

        public static async Task<ObservableCollection<VKWallPost>> LoadWallPosts(long groupID, int offset, int count)
        {
            var WallPosts = new ObservableCollection<VKWallPost>();

            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"owner_id", "-" + groupID },
                {"extended", "1" },
                {"offset", offset.ToString() },
                {"count", count.ToString() },
                {"access_token", await GetAccessToken() },
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

        public static async Task<ObservableCollection<VKAudio>> LoadMyAudio()
        {
            HttpClient http = new HttpClient();
            var q = new QueryString()
            {
                {"count", "100" },
                {"access_token", await GetAccessToken() },
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

        private static VKWallPost FormatPost(VKWallPost post)
        {

            #region Tags
            post.tags = new List<VKTag>();
            post.tags.AddRange(FormatTags(post.text));
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
                        if (attachment.photo.text != null)
                        {
                            post.tags.AddRange(FormatTags(attachment.photo.text));
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

        private static List<VKTag> FormatTags(string text)
        {
            var lst = new List<VKTag>();
            Regex r = new Regex(@"(#)\w+(@)\w+");
            foreach (Match m in r.Matches(text))
            {
                var tag = m.ToString();
                var split = tag.Split('@');
                lst.Add(new VKTag
                {
                    tag = split[0].Substring(1),
                    domain = split[1]
                });
            }
            return lst;
        }

        private static async Task<string> GetAccessToken()
        {
            //return VKSDK.GetAccessToken().AccessToken;
            var localSettings = ApplicationData.Current.LocalSettings;
            var tok = localSettings.Values["token"];
            if (tok == null)
                return await GetToken(null, null);
            else
                return tok.ToString();
        }

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
