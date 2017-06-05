using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.Util;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace VK.WindowsPhone.SDK.API.Model
{

    public class VKAudioRoot
    {
        public Response response { get; set; }

        public class Response
        {
            public int count { get; set; }
            public ObservableCollection<VKAudio> items { get; set; }
        }
    }

    public class VKAudioByIdRoot
    {
        public ObservableCollection<VKAudio> response { get; set; }
    }

    public partial class VKAudio : INotifyPropertyChanged
    {
        private bool m_IsSaved;
        public bool IsSaved
        {
            get { return m_IsSaved; }
            set
            {
                if (m_IsSaved == value)
                    return;
                m_IsSaved = value;
                if (PropertyChanged == null)
                    return;
                PropertyChanged(this, new PropertyChangedEventArgs("IsSaved"));
            }
        }

        private bool m_IsOffline;
        public bool IsOffline
        {
            get { return m_IsOffline; }
            set
            {
                if (m_IsOffline == value)
                    return;
                m_IsOffline = value;
                if (PropertyChanged == null)
                    return;
                PropertyChanged(this, new PropertyChangedEventArgs("IsOffline"));
            }
        }

        private bool m_IsPlaying;
        public bool IsPlaying
        {
            get { return m_IsPlaying; }
            set
            {
                if (m_IsPlaying == value)
                    return;
                m_IsPlaying = value;
                if (PropertyChanged == null)
                    return;
                PropertyChanged(this, new PropertyChangedEventArgs("IsPlaying"));
            }
        }


        public long id { get; set; }

        public long owner_id { get; set; }

        private string _artist = "";

        public string artist
        {
            get { return _artist; }
            set
            {
                _artist = (value ?? "").ForUI();
                // do not allow new line
                _artist = _artist.MakeIntoOneLine();

            }
        }

        private string _title = "";

        public event PropertyChangedEventHandler PropertyChanged;

        public string title
        {
            get { return _title; }
            set
            {
                _title = (value ?? "").ForUI();
                _title = _title.MakeIntoOneLine();
            }
        }

        public int duration { get; set; }

        public string url { get; set; }

        public string photo_url { get; set; }

        public long lyrics_id { get; set; }

        public long album_id { get; set;}

        public long genre_id { get; set; }

        private int m_dlProgress;
        public int dlProgress
        {
            get { return m_dlProgress; }
            set
            {
                if (m_dlProgress == value)
                    return;
                m_dlProgress = value;
                if (PropertyChanged == null)
                    return;
                PropertyChanged(this, new PropertyChangedEventArgs("dlProgress"));
            }
        }

        public async Task<StorageFile> DownloadTrack()
        {
            var folder = await KnownFolders.MusicLibrary.CreateFolderAsync("VKatcher", CreationCollisionOption.OpenIfExists);
            StorageFile sf = await folder.CreateFileAsync(title + "-" + artist + ".mp3", CreationCollisionOption.ReplaceExisting);

            var bgd = new BackgroundDownloader();
            var dlOp = bgd.CreateDownload(new Uri(url), sf);
            Progress<DownloadOperation> progress = new Progress<DownloadOperation>(OnDLProgressChanged);
            CancellationTokenSource cts = new CancellationTokenSource();

            await dlOp.StartAsync().AsTask(cts.Token, progress);
            return sf;
        }

        public async void DownloadTrackBG()
        {
            var folder = await KnownFolders.MusicLibrary.CreateFolderAsync("VKatcher", CreationCollisionOption.OpenIfExists);
            StorageFile sf = await folder.CreateFileAsync(title + "-" + artist + ".mp3", CreationCollisionOption.ReplaceExisting);

            var bgd = new BackgroundDownloader();
            var dlOp = bgd.CreateDownload(new Uri(url), sf);
            CancellationTokenSource cts = new CancellationTokenSource();

            await dlOp.StartAsync();
        }

        private void OnDLProgressChanged(DownloadOperation dlOP)
        {
            dlProgress = (int)(100 * ((double)dlOP.Progress.BytesReceived / (double)dlOP.Progress.TotalBytesToReceive));
            if (dlProgress >= 100)
            {
                dlProgress = 0;
                IsOffline = true;
                Debug.WriteLine("Downloaded " + title + " - " + artist);
            }
        }

        public async Task DeleteDownloadedTrack()
        {
            var sf = await StorageFile.GetFileFromPathAsync(url);
            await sf.DeleteAsync();
        }

        public async Task DeleteDownloadedTrack(string Path)
        {
            var sf = await StorageFile.GetFileFromPathAsync(Path);
            await sf.DeleteAsync();
        }
    }
}
