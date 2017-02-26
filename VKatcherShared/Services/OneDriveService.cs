using Microsoft.Toolkit.Uwp.Services.OneDrive;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using VKCatcherShared.Models;
using Windows.Storage;

namespace VKatcherShared.Services
{
    public class OneDriveService
    {
        public static string _host = "https://api.onedrive.com/v1.0/";

        public static async Task<bool> UploadFromUrl(string url, string name)
        {
            HttpClient client = new HttpClient();
            //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                await AuthenticationService.GetOneDriveAccessToken()
                );
            client.DefaultRequestHeaders.Add("Prefer", "respond-async");
            var uri = new Uri(_host + "drive/special/music/children");

            var j = JsonConvert.SerializeObject(new OneDriveUploadModel
            {
                @contentsourceUrl = url.Split('?')[0],
                name = name,
                file = new OneDriveUploadModel.File()
            });
            j = j.Replace("contentsourceUrl", "@content.sourceUrl");
            var res = await client.PostAsync(uri, new StringContent(j, Encoding.UTF8, "application/json"));
            var s = await res.Content.ReadAsStringAsync();

            var r = string.IsNullOrWhiteSpace(s);
            return r;
        }

        public static async Task InitializeAsync()
        {
            Microsoft.Toolkit.Uwp.Services.OneDrive.OneDriveService.Instance.Initialize("00000000481A8D3A", OneDriveEnums.AccountProviderType.Msa, OneDriveScopes.OfflineAccess | OneDriveScopes.ReadWrite);
            if (!await Microsoft.Toolkit.Uwp.Services.OneDrive.OneDriveService.Instance.LoginAsync())
            {
                Debug.WriteLine("Unable to log in to OneDrive");
            }
        }

        public static async Task<OneDriveStorageFolder> GetMusicFolder()
        {
            return await Microsoft.Toolkit.Uwp.Services.OneDrive.OneDriveService.Instance.MusicFolderAsync();
        }

        public static async Task SaveToOneDrive(VKAudio track)
        {
            var file = await track.DownloadTrack();
            var odFile = await UploadFile(file, track.title + " - " + track.artist + ".mp3");
            await track.DeleteDownloadedTrack(file.Path);
        }

        public static async Task<OneDriveStorageFile> UploadFile(StorageFile file, string name)
        {
            var folder = await GetMusicFolder();
            var largeFileCreated = await folder.UploadFileAsync(name, await file.OpenReadAsync(), CreationCollisionOption.OpenIfExists);
            return largeFileCreated;
        }
    }
}
