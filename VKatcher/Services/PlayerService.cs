using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Popups;

namespace VKatcher.Services
{
    public class PlayerService
    {
        #region Properties
        public static MediaPlayer MediaPlayer { get; set; } = new MediaPlayer();
        public static MediaPlaybackList CurrentPlaybackList { get; set; } = new MediaPlaybackList();
        public static ObservableCollection<VKAudio> CurrentPlaylist { get; set; } = new ObservableCollection<VKAudio>();
        #endregion

        public static void SetupPlayer()
        {
            MediaPlayer.Dispose();
            MediaPlayer = new MediaPlayer();

            CurrentPlaylist = new ObservableCollection<VKAudio>();

            CurrentPlaybackList = new MediaPlaybackList();
            CurrentPlaybackList.CurrentItemChanged += OnTrackChanged;

            MediaPlayer.Source = CurrentPlaybackList;
        }

        #region Methods

        private static void OnTrackChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            if (sender.CurrentItem != null)
            {
                var data = JsonConvert.SerializeObject(CurrentPlaylist[(int)sender.CurrentItemIndex]);
                AppDataService.SetRoamingSetting("current_track", data); 
            }
        }

        public static void BuildPlaylistFromCollection(ObservableCollection<VKAudio> audios, int playFrom, bool autoPlay)
        {
            MediaPlayer.Pause();
            CurrentPlaylist.Clear();
            CurrentPlaybackList.Items.Clear();
            foreach (var audio in audios)
            {
                CurrentPlaylist.Add(audio);
                var item = new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri(audio.url)));
                var props = item.GetDisplayProperties();
                props.Type = Windows.Media.MediaPlaybackType.Music;
                props.MusicProperties.Title = audio.title;
                props.MusicProperties.Artist = audio.artist;
                item.ApplyDisplayProperties(props);
                CurrentPlaybackList.Items.Add(item);
            }
            CurrentPlaybackList.MoveTo((uint)playFrom);

            var data = JsonConvert.SerializeObject(CurrentPlaylist);
            //AppDataService.SetRoamingSetting("current_playlist", data);
            if (autoPlay)
                MediaPlayer.Play();

        }

        public static void BuildPlaylistFromAudio(VKAudio audio, bool autoPlay)
        {
            MediaPlayer.Pause();
            CurrentPlaylist.Clear();
            CurrentPlaybackList.Items.Clear();

            CurrentPlaylist.Add(audio);
            var item = new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri(audio.url)));
            var props = item.GetDisplayProperties();
            props.Type = Windows.Media.MediaPlaybackType.Music;
            props.MusicProperties.Title = audio.title;
            props.MusicProperties.Artist = audio.artist;
            item.ApplyDisplayProperties(props);
            CurrentPlaybackList.Items.Add(item);

            var data = JsonConvert.SerializeObject(CurrentPlaylist);
            //AppDataService.SetRoamingSetting("current_playlist", data);
            if (autoPlay)
                MediaPlayer.Play();

        }

        public static void AddAudioToPlaylist(VKAudio audio)
        {
            CurrentPlaylist.Add(audio);
            var item = new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri(audio.url)));
            var props = item.GetDisplayProperties();
            props.Type = Windows.Media.MediaPlaybackType.Music;
            props.MusicProperties.Title = audio.title;
            props.MusicProperties.Artist = audio.artist;
            item.ApplyDisplayProperties(props);
            CurrentPlaybackList.Items.Add(item);

            var data = JsonConvert.SerializeObject(CurrentPlaylist);
            //AppDataService.SetRoamingSetting("current_playlist", data);
        }

        public static void PlayAudioNext(VKAudio audio)
        {
            CurrentPlaylist.Insert((int)CurrentPlaybackList.CurrentItemIndex + 1, audio);
            var item = new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri(audio.url)));
            var props = item.GetDisplayProperties();
            props.Type = Windows.Media.MediaPlaybackType.Music;
            props.MusicProperties.Title = audio.title;
            props.MusicProperties.Artist = audio.artist;
            item.ApplyDisplayProperties(props);
            CurrentPlaybackList.Items.Insert((int)CurrentPlaybackList.CurrentItemIndex +1, item);

            var data = JsonConvert.SerializeObject(CurrentPlaylist);
            //AppDataService.SetRoamingSetting("current_playlist", data);
        }
        #endregion
    }
}
