using Newtonsoft.Json;
using NotificationsExtensions;
using NotificationsExtensions.Toasts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK;
using VK.WindowsPhone.SDK.API.Model;
using VKatcherShared.Models;
using VKatcherShared.Services;
using Windows.ApplicationModel.Background;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.Notifications;

namespace VKBackground
{
    public sealed class CheckNewPostsTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        private AutoResetEvent _ARE;
        private ObservableCollection<VKGroup> SubscribedGroups;
        private const string _followedFile = "followed_groups.json";
        private const string _downloadedDB = "downloaded_files.json";
        private int _newTrackCount = 0;
        private StorageFile _subscribedFile;
        private StorageFile _downloadsFile;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Starting catcher task...");
            _deferral = taskInstance.GetDeferral();
            _ARE = new AutoResetEvent(false);
            VKSDK.Initialize("5545387");
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCancelled);
            VKSDK.WakeUpSession();
            var networkInfo = NetworkInformation.GetInternetConnectionProfile();
            if (networkInfo.IsWlanConnectionProfile)
            {
                Debug.WriteLine("WiFi connected");
                SubscribedGroups = new ObservableCollection<VKGroup>();
                await CheckSubscribedGroups();
                //CheckNewPosts();
                if (_newTrackCount > 0)
                {
                    PopToast();
                }
            }
            else
            {
                Debug.WriteLine("Not on WiFi...");
            }
            await SubscriptionService.WriteSubscribedGroups(SubscribedGroups);
            Debug.WriteLine("BG Task complete");
            _deferral.Complete();
        }

        private void OnCancelled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            string body = "The background task failed because " + reason;

            #region Toast Visual
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText() {Text = body},
                    },
                }
            };
            #endregion

            ToastContent toastContent = new ToastContent()
            {
                Visual = visual,
                ActivationType = ToastActivationType.Foreground,
            };

            var toast = new ToastNotification(toastContent.GetXml());
            toast.NotificationMirroring = NotificationMirroring.Allowed;
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private void PopToast()
        {

            string body = "We have " + _newTrackCount + " new tracks for you to check out!";

            #region Toast Visual
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText() {Text = body},
                    },
                }
            };
            #endregion

            ToastContent toastContent = new ToastContent()
            {
                Visual = visual,
                ActivationType = ToastActivationType.Foreground,
            };

            var toast = new ToastNotification(toastContent.GetXml());
            toast.NotificationMirroring = NotificationMirroring.Allowed;
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private async Task CheckSubscribedGroups()
        {
            #region Get Existing Downloads
            var dls = await FileService.GetDownloads();
            var lst = dls.ToList();
            #endregion
            SubscribedGroups = await SubscriptionService.LoadSubscribedGroups();
            Debug.WriteLine("Loaded subscribed groups");
            foreach (var group in SubscribedGroups)
            {
                #region Get Posts
                group.to_save = group.to_save == 0 ? 3 : group.to_save;
                var posts = await VKFacade.LoadWallPostsDIY(group.id, group.to_save, 0);
                Debug.WriteLine("Loaded posts from " + group.name);
                foreach (var post in posts)
                {
                    //Check last post in the group
                    if (post.id != group.last_id)
                    {
                        group.last_id = posts[0].id;
                        var attachments = post.attachments;
                        foreach (var a in attachments)
                        {
                            //Check if already downloaded
                            var t = lst.Find(x => x.id == a.audio.id);
                            if (t != null)
                            {
                                Debug.WriteLine("Download already exists");
                                break;
                            }
                            //Down't download huge files
                            if (a.audio.duration < 1200)
                            {
                                //Download the file
                                var file = await a.audio.DownloadTrack();
                                //Increment track count
                                _newTrackCount++;

                                #region Add track to database
                                if (file != null)
                                {
                                    FileService.WriteDownloads(a.audio, file);
                                    #endregion
                                }
                            }
                        }
                    }
                    else
                        break;
                    //Set the last post ID
                    group.last_id = posts[0].id;
                }
                Debug.WriteLine("The latest post was ID# " + posts[0].id); 
                #endregion
            }
        }

        #region Old
        private async void CheckNewPosts()
        {
            foreach (var group in SubscribedGroups)
            {
                group.to_save = group.to_save == null ? 3 : group.to_save;
                var posts = await VKFacade.LoadWallPostsDIY(group.id, group.to_save, 0);
                await CheckAttachments(posts, group);
            }
        }
        private async Task CheckAttachments(ObservableCollection<VKWallPost> posts, VKGroup group)
        {
            foreach (var post in posts)
            {
                if (post.id != group.last_id)
                {
                    post.id = group.last_id;
                    var attachments = post.attachments;
                    foreach (var a in attachments)
                    {
                        var file = await a.audio.DownloadTrack();
                        _newTrackCount++;
                        if (file != null)
                        {
                            try
                            {
                                var temp = await ApplicationData.Current.LocalFolder.GetFileAsync(_downloadedDB);
                                var str = File.ReadAllText(temp.Path);
                                var myDLs = JsonConvert.DeserializeObject<ObservableCollection<DownloadedTrack>>(str);
                                if (myDLs == null)
                                {
                                    myDLs = new ObservableCollection<DownloadedTrack>();
                                }
                                var d = new DownloadedTrack();
                                d.artist = a.audio.artist;
                                d.title = a.audio.title;
                                d.file_path = file.Path;
                                d.duration = a.audio.duration;

                                var tempcol = myDLs;
                                tempcol.Insert(0, d);
                                string newstr = JsonConvert.SerializeObject(tempcol);
                                File.WriteAllText(temp.Path, newstr);
                            }
                            catch (Exception ex)
                            {
                                //var file = await ApplicationData.Current.RoamingFolder.CreateFileAsync(_downloadedDB);
                                Debug.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }
        } 
        #endregion
    }
}
