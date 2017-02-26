using Microsoft.Toolkit.Uwp;
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
        private ObservableCollection<VKAudio> ToDownload;
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
            ToDownload = new ObservableCollection<VKAudio>();
            SubscribedGroups = new ObservableCollection<VKGroup>();
            await CheckSubscribedGroups();
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
            _deferral.Complete();
        }

        private async Task CheckSubscribedGroups()
        {
            SubscribedGroups = await SubscriptionService.LoadSubscribedGroups();
            Debug.WriteLine("Loaded subscribed groups");
            StorageFile dlFile;
            try
            {
                dlFile = await ApplicationData.Current.LocalFolder.GetFileAsync("DL_List.json");
            }
            catch (Exception)
            {
                dlFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("DL_List.json");
            }
            var dlList = JsonConvert.DeserializeObject<ObservableCollection<VKAudio>>(File.ReadAllText(dlFile.Path));
            await Task.Run(async () =>
            {
                foreach (var group in SubscribedGroups)
                {
                    #region Get Posts
                    group.to_save = group.to_save == 0 ? 3 : group.to_save;
                    var posts = await VKFacade.LoadWallPosts(group.id, group.to_save, 0);
                    Debug.WriteLine("Loaded posts from " + group.name);
                    foreach (var post in posts)
                    {
                        //Check last post in the group
                        if (post.id != group.last_id)
                        {
                            group.last_id = posts[0].id;
                            var attachments = post.attachments;
                            foreach (var att in attachments)
                            {
                                //Don't download huge files
                                if (att.audio.duration < 1200)
                                    ToDownload.Add(att.audio);
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
            });
            File.WriteAllText(dlFile.Path, JsonConvert.SerializeObject(ToDownload));
        }
    }
}
