using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API;
using VK.WindowsPhone.SDK.API.Model;
using VK.WindowsPhone.SDK.Util;
using VKatcher.ViewModels;
using VKatcher.Views;
using VKatcherShared.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace VKatcher.Services
{
    public class DataService
    {
        public static async Task<ObservableCollection<VKGroup>> LoadMyGroups()
        {
            var MyGroups = new ObservableCollection<VKGroup>();
            await Task.Run(() =>
            {
                VKRequest.Dispatch<VKList<VKGroup>>(new VKRequestParameters("groups.get",
                "extended", "1"),
                async (res) =>
                {
                    await VKExecute.ExecuteOnUIThread(() =>
                    {
                        if (res.ResultCode == VKResultCode.Succeeded)
                        {
                            var groupList = res.Data.items;
                            foreach (var group in groupList)
                            {
                                MyGroups.Add(group);
                            }
                        }
                    });
                });
            });
            return MyGroups;
        }

        public static async Task<ObservableCollection<VKGroup>> SearchGroups(string query)
        {
            var MyGroups = new ObservableCollection<VKGroup>();
            await Task.Run(() =>
            {
                VKRequest.Dispatch<VKList<VKGroup>>(new VKRequestParameters("groups.search",
                    "q", query,
                    "type", "group"),
                    async (res) =>
                    {
                        await VKExecute.ExecuteOnUIThread(() =>
                        {
                            if (res.ResultCode == VKResultCode.Succeeded)
                            {
                                var groupList = res.Data.items;
                                foreach (var group in groupList)
                                {
                                    MyGroups.Add(group);
                                }
                            }
                        });
                    });
            });
            return MyGroups;
        }

        public static async Task<ObservableCollection<VKAudio>> SearchAudio(string query)
        {
            var MyAudio = new ObservableCollection<VKAudio>();
            var vm = ((Window.Current.Content as Frame).Content as MainPage).DataContext as MainViewModel;

            await Task.Run(() =>
            {
                VKRequest.Dispatch<VKList<VKAudio>>(new VKRequestParameters("audio.search",
                    "q", query,
                    "auto_complete", "true"),
                    async (res) =>
                    {
                        await VKExecute.ExecuteOnUIThread(() =>
                        {
                            if (res.ResultCode == VKResultCode.Succeeded)
                            {
                                var tracks = res.Data.items;
                                foreach (var track in tracks)
                                {
                                    var t = vm._currentTrack;
                                    if (t != null &&
                                        track.id == t.id)
                                    {
                                        track.IsPlaying = true;
                                        App.ViewModelLocator.Feed._selectedTrack = track;
                                    }
                                    else
                                        track.IsPlaying = false;
                                    MyAudio.Add(track);
                                }
                            }
                        });
                    });
            });
            return MyAudio;
        }

        public static async Task<ObservableCollection<VKWallPost>> LoadWallPosts(long groupID, int offset, int count)
        {
            var WallPosts = new ObservableCollection<VKWallPost>();

            await Task.Run(() =>
            {
                var param = new VKRequestParameters("wall.get",
                "owner_id", "-" + groupID.ToString(),
                "extended", "1",
                "offset", offset.ToString(),
                "count", count.ToString());
                VKRequest.Dispatch<VKList<VKWallPost>>(
                   param, async (res) =>
                   {
                       await VKExecute.ExecuteOnUIThread(async () =>
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
                                           temp = await FormatPost(temp);
                                       }
                                       else if (temp.copy_history != null
                                       && temp.copy_history.Count > 0)
                                       {
                                           temp = await FormatPost(temp.copy_history[0]);
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
            });

            return WallPosts;
        }

        public static async Task<ObservableCollection<VKAudio>> LoadMyAudio()
        {
            var dls = await FileService.GetDownloads();
            var lst = dls.ToList();
            var myAudio = new ObservableCollection<VKAudio>();
            var vm = ((Window.Current.Content as Frame).Content as MainPage).DataContext as MainViewModel;
            await Task.Run(() =>
            {
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
                                        var item = track;
                                        var tr = lst.Find(x => x.id == item.id);
                                        if (tr != null)
                                        {
                                            item = tr;
                                        }
                                        item.IsSaved = true;
                                        var t = vm._currentTrack;
                                        if (t != null &&
                                            item.id == t.id)
                                        {
                                            item.IsPlaying = true;
                                            App.ViewModelLocator.Feed._selectedTrack = item;
                                        }
                                        else
                                            item.IsPlaying = false;
                                        myAudio.Add(item);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }
                        });
                    });
            });
            return myAudio;
        }

        private static async Task<VKWallPost> FormatPost(VKWallPost post)
        {
            var dls = await FileService.GetDownloads();
            var lst = dls.ToList();

            var vm = ((Window.Current.Content as Frame).Content as MainPage).DataContext as MainViewModel;
            for (int i = post.attachments.Count - 1; i > -1; i--)
            {
                var attachment = post.attachments[i];
                switch (attachment.type)
                {
                    case "audio":
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
                        post.attachments.RemoveAt(i);
                        break;
                    default:
                        post.attachments.RemoveAt(i);
                        //Or else remove the attachment
                        break;
                        #endregion
                }
            }
            foreach (var audio in post.attachments)
            {
                audio.audio.photo_url = post.post_image;
                #region Check if downloaded
                var item = lst.Find(x => x.id == audio.audio.id);
                if (item != null)
                {
                    audio.audio = item;
                } 
                #endregion
                #region Check if playing
                var playingTrack = vm._currentTrack;
                if (playingTrack != null &&
                    audio.audio.id == playingTrack.id)
                {
                    audio.audio.IsPlaying = true;
                    App.ViewModelLocator.Feed._selectedTrack = audio.audio;
                }
                else
                    audio.audio.IsPlaying = false;
                #endregion
            }
            return post;
        }

    }
}
