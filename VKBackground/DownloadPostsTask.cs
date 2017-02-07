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
    public sealed class DownloadPostsTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        private AutoResetEvent _ARE;
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

        private async Task DownloadPosts()
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
                    {
                        Debug.WriteLine("Download already exists");
                    }
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
    }
}
