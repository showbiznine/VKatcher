using Microsoft.Toolkit.Uwp;
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
using Windows.Networking.BackgroundTransfer;
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
        public static ObservableCollection<VKTag> SubscribedTags { get; private set; }

        #region Catcher task
        public static async Task DownloadAudios(IBackgroundTaskInstance taskInstance)
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

                var res = await DownloadPosts();
                if (res is int && (int)res > 0)
                    NotificationService.PopToastGeneric((int)res);
                //else if (res is VKAudio && (VKAudio)res != null)
                    //NotificationService.PopToastTrack((VKAudio)res);
            }
            else
            {
                Debug.WriteLine("Not on WiFi...");
            }
            Debug.WriteLine("Download Posts Task complete");
            _deferral.Complete();
        }

        private static async Task<object> DownloadPosts()
        {

            var completionGroup = new BackgroundTransferCompletionGroup();
            var dlPostTask = BackgroundTaskHelper.Register("DownloadPostProcessing",
                    completionGroup.Trigger);

            int max = 1;
            int count = 0;
            VKAudio lastDL = null;
            var dlFile = await ApplicationData.Current.LocalFolder.GetFileAsync(Constants.DownloadList);
            if (dlFile == null)
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync(Constants.DownloadList);
            }

            var dlText = await StorageFileHelper.ReadTextFromFileAsync(ApplicationData.Current.LocalFolder, Constants.DownloadList);
            ToDownload = JsonConvert.DeserializeObject<ObservableCollection<VKAudio>>(dlText);

            #region Get Existing Downloads
            var dls = await FileService.GetDownloads();
            var lst = dls.ToList();
            #endregion

            if (ToDownload.Count > 0)
            {
                for (int i = 0; i < max; i++)
                {
                    //Check if already downloaded
                    var t = lst.Find(x => x.id == ToDownload[i].id);
                    if (t != null)
                    {
                        Debug.WriteLine("Download already exists");
                        max++;
                    }
                    else
                    {
                        //Don't download huge files
                        if (ToDownload[i].duration < 1200)
                        {
                            //Download the file
                            var dl = await DownloadTrackBG(ToDownload[i], completionGroup);

                            //Increment track count
                            count++;
                            lastDL = ToDownload[i];

                            #region Add track to database
                            if (dl != null)
                                FileService.WriteDownloads(ToDownload[i], dl);
                            #endregion
                        }
                    }
                    ToDownload.Remove(ToDownload[i]);
                }
                var newList = JsonConvert.SerializeObject(ToDownload);
                var newFile = await StorageFileHelper.WriteTextToFileAsync(AppDataService.LocalFolder, newList, Constants.DownloadList, CreationCollisionOption.ReplaceExisting);

                if (count > 1)
                    return count;
                else
                    return lastDL;
            }

            return null;
        }

        private static async Task<StorageFile> DownloadTrackBG(VKAudio audio, BackgroundTransferCompletionGroup completionGroup)
        {
            var folder = await KnownFolders.MusicLibrary.CreateFolderAsync("VKatcher", CreationCollisionOption.OpenIfExists);
            StorageFile sf = await folder.CreateFileAsync(audio.title + "-" + audio.artist + ".mp3", CreationCollisionOption.ReplaceExisting);

            var bgd = new BackgroundDownloader(completionGroup);
            bgd.SuccessToastNotification = new ToastNotification(NotificationService.PopToastTrack(audio).Content);
            var dlOp = bgd.CreateDownload(new Uri(audio.url), sf);
            CancellationTokenSource cts = new CancellationTokenSource();
            await dlOp.StartAsync();
            return sf;
        }
        #endregion

        #region Check posts task
        public static async Task CheckNewPosts(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Starting catcher task...");
            _deferral = taskInstance.GetDeferral();
            _ARE = new AutoResetEvent(false);
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCancelled);
            var networkInfo = NetworkInformation.GetInternetConnectionProfile();
            ToDownload = new ObservableCollection<VKAudio>();
            SubscribedGroups = new ObservableCollection<VKGroup>();
            SubscribedTags = new ObservableCollection<VKTag>();
            await CheckSubscribedGroups();
            await CheckSubscribedTags();
            await SubscriptionService.WriteSubscribedGroups(SubscribedGroups);
            Debug.WriteLine("Check Posts Task complete");
            _deferral.Complete();
        }

        private static async Task CheckSubscribedTags()
        {
            SubscribedTags = await SubscriptionService.LoadSubscribedTags();
            Debug.WriteLine("Loaded subscribed tags");
            StorageFile dlFile;
            try
            {
                dlFile = await ApplicationData.Current.LocalFolder.GetFileAsync(Constants.DownloadList);
            }
            catch (Exception)
            {
                dlFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(Constants.DownloadList);
            }
            var dlList = JsonConvert.DeserializeObject<ObservableCollection<VKAudio>>(File.ReadAllText(dlFile.Path));
            foreach (var tag in SubscribedTags)
            {
                #region Get Posts
                tag.to_save = tag.to_save == 0 ? 3 : tag.to_save;
                var posts = await DataService.SearchWallByTag(tag.tag, tag.domain, tag.to_save);
                Debug.WriteLine("Loaded posts from #" + tag.tag + "@" + tag.domain);
                foreach (var post in posts)
                {
                    //Check last post in the group
                    if (post.id != tag.LastSavedId)
                    {
                        tag.LastSavedId = posts[0].id;
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
                    tag.LastSavedId = posts[0].id;
                }
                Debug.WriteLine("The latest post was ID# " + posts[0].id);
                #endregion
            }
            File.WriteAllText(dlFile.Path, JsonConvert.SerializeObject(ToDownload));
        }

        private static async Task CheckSubscribedGroups()
        {
            SubscribedGroups = await SubscriptionService.LoadSubscribedGroups();
            Debug.WriteLine("Loaded subscribed groups");
            StorageFile dlFile;
            try
            {
                var dlText = await StorageFileHelper.ReadTextFromFileAsync(ApplicationData.Current.LocalFolder, Constants.DownloadList);
                var dlList = JsonConvert.DeserializeObject<ObservableCollection<VKAudio>>(dlText);
            }
            catch (Exception)
            {
                dlFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(Constants.DownloadList, CreationCollisionOption.ReplaceExisting);
            }
            int count = 0;
            foreach (var group in SubscribedGroups)
            {
                #region Get Posts
                group.to_save = group.to_save == 0 ? 3 : group.to_save;
                var posts = await DataService.LoadWallPosts(group.id, 0, group.to_save);
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
                            {
                                ToDownload.Add(att.audio);
                                count++;
                            }
                        }
                    }
                    else
                        break;
                    //Set the last post ID
                    group.last_id = posts[0].id;
                }
                Debug.WriteLine("The latest post was ID# " + posts[0].id);
                if (count > 0)
                    NotificationService.PopToastCommunity(group, count);
                #endregion
            }
            var newText = JsonConvert.SerializeObject(ToDownload);
            await StorageFileHelper.WriteTextToFileAsync(ApplicationData.Current.LocalFolder, newText, Constants.DownloadList, CreationCollisionOption.ReplaceExisting);
        }
        #endregion

        #region Download Post-processing
        public static void ProcessDownloads(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Starting post-processing task...");
            _deferral = taskInstance.GetDeferral();
            var details = (BackgroundTransferCompletionGroupTriggerDetails)taskInstance.TriggerDetails;
            var downloads = details.Downloads;

            if (details.Downloads[0].ResultFile != null)
            {

            }
            _deferral.Complete();
        }
        #endregion

        private static void OnCancelled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            throw new NotImplementedException();
        }
    }
}
