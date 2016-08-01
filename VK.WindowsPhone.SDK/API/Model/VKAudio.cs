﻿using System;
using System.Collections.Generic;
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

        private int m_Progress;
        public int Progress
        {
            get { return m_Progress; }
            set
            {
                if (m_Progress == value)
                    return;
                m_Progress = value;
                if (PropertyChanged == null)
                    return;
                PropertyChanged(this, new PropertyChangedEventArgs("Progress"));
            }
        }

        public async Task<StorageFile> DownloadTrack()
        {
            var folder = await KnownFolders.MusicLibrary.GetFolderAsync("VKatcher");
            StorageFile sf = await folder.CreateFileAsync(title + "-" + artist + ".mp3", CreationCollisionOption.ReplaceExisting);

            var bgd = new BackgroundDownloader();
            var dlOp = bgd.CreateDownload(new Uri(url), sf);
            Progress<DownloadOperation> progress = new Progress<DownloadOperation>(OnDLProgressChanged);
            CancellationTokenSource cts = new CancellationTokenSource();

            await dlOp.StartAsync().AsTask(cts.Token, progress);
            return sf;
        }

        private void OnDLProgressChanged(DownloadOperation dlOP)
        {
            Progress = (int)(100 * (dlOP.Progress.BytesReceived / dlOP.Progress.TotalBytesToReceive));
            if (Progress >= 100)
            {
                Progress = 0;
                Debug.WriteLine("Downloaded " + title + " - " + artist);
            }
        }

        public async Task DeleteDownloadedTrack()
        {
            var sf = await StorageFile.GetFileFromPathAsync(url);
            await sf.DeleteAsync();
        }
    }
}