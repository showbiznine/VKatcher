using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using Windows.Storage;

namespace VKatcherShared.Services
{
    public class FileService
    {
        private const string _downloadedDB = "downloaded_files.json";
        private const string _dlList = "DL_List.json";

        public static async Task<ObservableCollection<VKAudio>> GetDownloads()
        {
            var dls = new ObservableCollection<VKAudio>();
            try
            {
                var str = await StorageFileHelper.ReadTextFromLocalFileAsync(_downloadedDB);
                var temp = JsonConvert.DeserializeObject<ObservableCollection<VKAudio>>(str);
                if (temp != null)
                {
                    foreach (var item in temp)
                    {
                        StorageFile f = null;
                        try
                        {
                            f = await StorageFile.GetFileFromPathAsync(item.url);
                        }
                        catch (Exception)
                        {
                            break;
                        }
                        if (f != null)
                        {
                            dls.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var file = await StorageFileHelper.WriteTextToLocalFileAsync(string.Empty, _downloadedDB, CreationCollisionOption.ReplaceExisting);
                Debug.WriteLine(ex.Message);
            }
            await WriteDownloads(dls);
            return dls;
        }

        public static async Task DeleteDownload(VKAudio track)
        {
            var dls = await GetDownloads();
            await track.DeleteDownloadedTrack();
            var t = dls.First(x => x.id == track.id);
            dls.Remove(t);
            await WriteDownloads(dls);
            Debug.WriteLine("Deleted track");
        }

        public static async Task WriteDownloads(ObservableCollection<VKAudio> dls)
        {
            StorageFile dlFile;
            try
            {
                dlFile = await ApplicationData.Current.LocalFolder.GetFileAsync(_downloadedDB);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                dlFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(_downloadedDB);
                Debug.WriteLine("Created new file");
            }
            if (dlFile != null)
            {
                string newstr = JsonConvert.SerializeObject(dls);
                File.WriteAllText(dlFile.Path, newstr);
                Debug.WriteLine("Wrote to database");
            }
        }

        public static async Task WriteDownloads(VKAudio track, StorageFile file)
        {
            try
            {
                var str = await StorageFileHelper.ReadTextFromLocalFileAsync(_downloadedDB);
                var myDLs = JsonConvert.DeserializeObject<ObservableCollection<VKAudio>>(str);
                if (myDLs == null)
                {
                    myDLs = new ObservableCollection<VKAudio>();
                }

                track.url = file.Path;

                var tempcol = myDLs;
                tempcol.Insert(0, track);
                string newstr = JsonConvert.SerializeObject(tempcol);
                var dlFile = await StorageFileHelper.WriteTextToLocalFileAsync(newstr, _downloadedDB, CreationCollisionOption.ReplaceExisting);
                Debug.WriteLine("Wrote to database");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                var dlFile = await StorageFileHelper.WriteTextToLocalFileAsync(string.Empty, _downloadedDB, CreationCollisionOption.ReplaceExisting);
                Debug.WriteLine("Created new file");
            }
        }

        public static async void ClearDownloads()
        {
            StorageFile dlFile;
            try
            {
                dlFile = await ApplicationData.Current.LocalFolder.GetFileAsync(_downloadedDB);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                dlFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(_downloadedDB);
                Debug.WriteLine("Created new file");
            }
            if (dlFile != null)
            {
                var empty = new ObservableCollection<VKAudio>();
                string newstr = JsonConvert.SerializeObject(empty);
                File.WriteAllText(dlFile.Path, newstr);
                Debug.WriteLine("Wrote to database");
            }
        }

        public static async Task ClearToDownload()
        {
            StorageFile dlFile;
            try
            {
                dlFile = await ApplicationData.Current.LocalFolder.GetFileAsync(_dlList);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                dlFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(_dlList);
                Debug.WriteLine("Created new file");
            }
            if (dlFile != null)
            {
                var empty = new ObservableCollection<VKAudio>();
                string newstr = JsonConvert.SerializeObject(empty);
                File.WriteAllText(dlFile.Path, newstr);
                Debug.WriteLine("Wrote to database");
            }
        }


        //public static async void OneDriveUpload(string url)
        //{
        //    var oneDriveClient = new OneDriveClient(null);
        //    var uploadedItem = await oneDriveClient.Drive.Root.ItemWithPath(url).Content.Request().PutAsync<>
        //}
    }
}
