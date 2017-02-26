using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using VKatcherShared.Services;
using Windows.ApplicationModel.Background;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.Notifications;

namespace VKatcher.Services
{
    public class BackgroundTaskService
    {
        private static string _followedFile = "followed_groups.json";
        private static string _downloadedDB = "downloaded_files.json";
        private static BackgroundTaskDeferral _deferral;
        private static AutoResetEvent _ARE;
        private static int _newTrackCount;

        public static ObservableCollection<VKAudio> ToDownload { get; private set; }
        public static ObservableCollection<VKGroup> SubscribedGroups { get; private set; }

        #region Catcher task
        public static async void DownloadAudios(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Starting catcher task...");
            _deferral = taskInstance.GetDeferral();
            _ARE = new AutoResetEvent(false);
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCancelled);
            var networkInfo = NetworkInformation.GetInternetConnectionProfile();
            ToDownload = new ObservableCollection<VKAudio>();
            if (networkInfo.IsWlanConnectionProfile)
            {
                Debug.WriteLine("WiFi connected");
                await DownloadPosts();
                if (_newTrackCount > 0)
                {
                    PopToast();
                }
            }
            else
            {
                Debug.WriteLine("Not on WiFi...");
            }
            Debug.WriteLine("BG Task complete");
            _deferral.Complete();
        }

        private static void PopToast()
        {
            string body;
            if (_newTrackCount > 1)
                body = "We have " + _newTrackCount + " new tracks for you to check out!";
            else
                body = "We have a new track for you to check out!";

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
                Launch = "downloads"
            };

            var toast = new ToastNotification(toastContent.GetXml());
            toast.NotificationMirroring = NotificationMirroring.Allowed;
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private static async Task DownloadPosts()
        {
            var dlFile = await ApplicationData.Current.LocalFolder.GetFileAsync("DL_List.json");
            if (dlFile == null)
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync("DL_List.json");
            }
            ToDownload = JsonConvert.DeserializeObject<ObservableCollection<VKAudio>>(File.ReadAllText(dlFile.Path));

            #region Get Existing Downloads
            var dls = await FileService.GetDownloads();
            var lst = dls.ToList();
            #endregion

            if (ToDownload.Count > 0)
            {
                for (int i = 0; i < 1; i++)
                {
                    //Check if already downloaded
                    var t = lst.Find(x => x.id == ToDownload[i].id);
                    if (t != null)
                        Debug.WriteLine("Download already exists");
                    else
                    {
                        //Don't download huge files
                        if (ToDownload[i].duration < 1200)
                        {
                            //Download the file
                            var file = await ToDownload[i].DownloadTrackB();

                            //Increment track count
                            _newTrackCount++;

                            #region Add track to database
                            if (file != null)
                            {
                                FileService.WriteDownloads(ToDownload[i], file);
                                #endregion
                            }
                        }
                    }
                    ToDownload.Remove(ToDownload[i]);
                }
                File.WriteAllText(dlFile.Path, JsonConvert.SerializeObject(ToDownload));
            }
        }
        #endregion

        #region Check posts task
        public static async void CheckNewPosts(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Starting catcher task...");
            _deferral = taskInstance.GetDeferral();
            _ARE = new AutoResetEvent(false);
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCancelled);
            var networkInfo = NetworkInformation.GetInternetConnectionProfile();
            ToDownload = new ObservableCollection<VKAudio>();
            SubscribedGroups = new ObservableCollection<VKGroup>();
            await CheckSubscribedGroups();
            await SubscriptionService.WriteSubscribedGroups(SubscribedGroups);
            Debug.WriteLine("BG Task complete");
            _deferral.Complete();
        }

        private static async Task CheckSubscribedGroups()
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
                    var posts = await DataService.LoadWallPosts(group.id, group.to_save, 0);
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

        #endregion

        private static void OnCancelled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            throw new NotImplementedException();
        }
    }
}
