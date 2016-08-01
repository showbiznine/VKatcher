using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VK.WindowsPhone.SDK.API.Model;
using VKatcher.Services;
using VKatcher.Views;
using VKatcherShared.Messages;
using VKatcherShared.Services;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace VKatcher.ViewModels
{
    public class FeedPageViewModel : ViewModelBase
    {
        #region Fields
        public VKGroup _currentGroup { get; set; }
        public ObservableCollection<VKWallPost> _wallPosts { get; set; }
        public int _offset { get; set; }
        public bool _inCall { get; set; }
        //public static VKAudio _selectedTrack;
        //public static ObservableCollection<VKAudio> _currentPlaylist;
        public VKAudio _selectedTrack;
        public ObservableCollection<VKAudio> _currentPlaylist;
        private MediaPlayer _mediaPlayer;
        private const string _downloadedDB = "downloaded_files.json";
        #endregion

        #region Commands
        public RelayCommand LoadPostsCommand { get; private set; }
        public RelayCommand RefreshPostsCommand { get; private set; }
        public RelayCommand SubscribeCommand { get; private set; }
        public RelayCommand<Grid> DownloadTrackCommand { get; private set; }
        public RelayCommand<Grid> DeleteDownloadCommand { get; private set; }
        public RelayCommand<Grid> AddToPlaylistCommand { get; private set; }
        public RelayCommand<Grid> PlayNextCommand { get; private set; }
        public RelayCommand<ItemClickEventArgs> SongListViewItemClickCommand { get; private set; }
        public RelayCommand<object> SongHoldingCommand { get; private set; }
        public RelayCommand<object> SongRightTappedCommand { get; private set; }

        #endregion

        public FeedPageViewModel()
        {
            if (IsInDesignMode)
            {
                _currentGroup = new VKGroup()
                {
                    name = "Test Group",
                    IsSubscribed = true
                };
            }
            else
            {
                InitializeCommands();
                _mediaPlayer = BackgroundMediaPlayer.Current;
            }
        }

        private void InitializeCommands()
        {
            LoadPostsCommand = new RelayCommand(() => LoadPosts(_offset, 20));
            RefreshPostsCommand = new RelayCommand(() => LoadPosts(_offset, 10));
            DownloadTrackCommand = new RelayCommand<Grid>(grid =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    var att = grid.DataContext as VKAttachment;
                    DownloadTrack(att.audio);
                });
            });
            DeleteDownloadCommand = new RelayCommand<Grid>(grid =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var att = grid.DataContext as VKAttachment;
                    if (att.audio.IsPlaying)
                    {
                        MessageService.SendMessageToBackground(new SkipNextMessage());
                    }
                    await FileService.DeleteDownload(att.audio);
                    att.audio.IsOffline = false;
                });
            });
            AddToPlaylistCommand = new RelayCommand<Grid>(grid =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    var att = grid.DataContext as VKAttachment;
                    App.ViewModelLocator.Main._currentPlaylist.Add(att.audio);
                    MessageService.SendMessageToBackground(new AddToPlaylistMessage(att.audio));
                });
            });
            PlayNextCommand = new RelayCommand<Grid>(grid =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    var att = grid.DataContext as VKAttachment;
                    var lst = App.ViewModelLocator.Main._currentPlaylist.ToList();
                    var i = lst.FindIndex(0, lst.Count, x => x.id == App.ViewModelLocator.Main._currentTrack.id);
                    App.ViewModelLocator.Main._currentPlaylist.Insert(i + 1, att.audio);
                    MessageService.SendMessageToBackground(new PlayNextMessage(att.audio));
                });
            });
            SongListViewItemClickCommand = new RelayCommand<ItemClickEventArgs>(args => OnSongListItemClick(args));
            SubscribeCommand = new RelayCommand(async () =>
            {
                var sub = await SubscriptionService.SubscribeToGroup(_currentGroup);
            });
            SongHoldingCommand = new RelayCommand<object>(sender =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    Grid obj = (Grid)sender;
                    FlyoutBase.ShowAttachedFlyout(obj);
                });
            });
            SongRightTappedCommand = new RelayCommand<object>(sender =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    Grid obj = (Grid)sender;
                    FlyoutBase.ShowAttachedFlyout(obj);
                });
            });
        }

        public async void LoadPosts(int offset, int count)
        {
            if (_wallPosts == null)
            {
                await DispatcherHelper.RunAsync(async () =>
                {
                    _inCall = true;
                    var temp = await DataService.LoadWallPosts(_currentGroup.id, offset, count);
                    temp.CollectionChanged += (s, e) =>
                    {
                        if (temp.Count > 0)
                            _wallPosts = temp;
                    };
                    _inCall = false;
                });
            }
        }

        private async void DownloadTrack(VKAudio track)
        {
            var file = await track.DownloadTrack();
            if (file != null)
            {
                FileService.WriteDownloads(track, file);
            }
        }

        public async void OnSongListItemClick(ItemClickEventArgs e)
        {
            bool containsOffline = false;
            if (e.ClickedItem is VKAttachment)
            {
                if (_selectedTrack != null)
                {
                    _selectedTrack.IsPlaying = false;
                }
                _selectedTrack = (e.ClickedItem as VKAttachment).audio;
                Debug.WriteLine("Clicked " + _selectedTrack.title);
                if (App.ViewModelLocator.Main._currentTrack != null)
                {
                    App.ViewModelLocator.Main._currentTrack.IsPlaying = false;
                }
                _selectedTrack.IsPlaying = true;
                App.ViewModelLocator.Main._currentTrack = _selectedTrack;

                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    ListView lst = e.OriginalSource as ListView;
                    _currentPlaylist = new ObservableCollection<VKAudio>();
                    foreach (var item in lst.Items)
                    {
                        if ((item as VKAttachment).audio.IsOffline)
                        {
                            containsOffline = true;
                        }
                        _currentPlaylist.Add((item as VKAttachment).audio);
                    }
                    App.ViewModelLocator.Main._currentPlaylist = _currentPlaylist;
                });

                MessageService.SendMessageToBackground(new UpdatePlaylistMessage(App.ViewModelLocator.Main._currentPlaylist));
                if (containsOffline)
                {
                    await Task.Delay(500); 
                }
                MessageService.SendMessageToBackground(new TrackChangedMessage(new Uri(_selectedTrack.url)));
                MessageService.SendMessageToBackground(new StartPlaybackMessage());
            }
        }
    }
}
