using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using Windows.UI.Popups;
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
        private int _CurrentIndex;
        #endregion

        #region Commands
        public RelayCommand ToggleMenuCommand { get; private set; }
        public RelayCommand LoadPostsCommand { get; private set; }
        public RelayCommand RefreshPostsCommand { get; private set; }
        public RelayCommand SubscribeCommand { get; private set; }
        public RelayCommand<Grid> DownloadTrackCommand { get; private set; }
        public RelayCommand<Grid> DeleteDownloadCommand { get; private set; }
        public RelayCommand<Grid> AddToPlaylistCommand { get; private set; }
        public RelayCommand<Grid> PlayNextCommand { get; private set; }
        public RelayCommand<ItemClickEventArgs> SongListViewItemClickCommand { get; private set; }
        public RelayCommand<LinkClickedEventArgs> TagClickCommand { get; private set; }
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
                _wallPosts = new ObservableCollection<VKWallPost>();
                _mediaPlayer = BackgroundMediaPlayer.Current;
            }
        }

        private void InitializeCommands()
        {
            ToggleMenuCommand = new RelayCommand(() => App.ViewModelLocator.Main.IsMenuOpen = !App.ViewModelLocator.Main.IsMenuOpen);
            LoadPostsCommand = new RelayCommand(() => LoadPosts(_offset, 30, true));
            RefreshPostsCommand = new RelayCommand(() => LoadPosts(_offset, 30, true));
            DownloadTrackCommand = new RelayCommand<Grid>(grid =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    var att = grid.DataContext as VKAttachment;
                    DownloadTrack(att.audio);
                });
            });
            DeleteDownloadCommand = new RelayCommand<Grid>(grid =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(async () =>
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
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    var att = grid.DataContext as VKAttachment;
                    App.ViewModelLocator.Main._currentPlaylist.Add(att.audio);
                    MessageService.SendMessageToBackground(new AddToPlaylistMessage(att.audio));
                });
            });
            PlayNextCommand = new RelayCommand<Grid>(grid =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    var att = grid.DataContext as VKAttachment;
                    var lst = App.ViewModelLocator.Main._currentPlaylist.ToList();
                    var i = lst.FindIndex(0, lst.Count, x => x.id == App.ViewModelLocator.Main._currentTrack.id);
                    App.ViewModelLocator.Main._currentPlaylist.Insert(i + 1, att.audio);
                    MessageService.SendMessageToBackground(new PlayNextMessage(att.audio));
                });
            });
            SongListViewItemClickCommand = new RelayCommand<ItemClickEventArgs>(
                args =>
                {
                    OnSongListItemClick(args);
                });
            TagClickCommand = new RelayCommand<LinkClickedEventArgs>(args =>
            {
                OnTagClick(args.Link);
            });
            SubscribeCommand = new RelayCommand(async () =>
            {
                var sub = await SubscriptionService.SubscribeToGroup(_currentGroup);
            });
            SongHoldingCommand = new RelayCommand<object>(sender =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    Grid obj = (Grid)sender;
                    FlyoutBase.ShowAttachedFlyout(obj);
                });
            });
            SongRightTappedCommand = new RelayCommand<object>(sender =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    Grid obj = (Grid)sender;
                    FlyoutBase.ShowAttachedFlyout(obj);
                });
            });
        }

        private async void OnTagClick(string tag)
        {
            _inCall = true;
            _wallPosts.Clear();
            var split = tag.Split('#', '@');
            List<string> fixedSplit = new List<string>();
            foreach (var t in split)
            {
                if (!string.IsNullOrWhiteSpace(t))
                    fixedSplit.Add(t);
            }
            foreach (var post in await DataService.SearchWallByTag(fixedSplit[0], fixedSplit.Count > 1 ? fixedSplit[1] : _currentGroup.screen_name))
            {
                _wallPosts.Add(post);
            }
            _inCall = false;
        }

        public async void LoadPosts(int offset, int count, bool clear)
        {
            _inCall = true;
            try
            {
                if (_wallPosts != null && clear)
                {
                    _wallPosts.Clear();
                }
                foreach (var post in await DataService.LoadWallPosts(_currentGroup.id, offset, count))
                {
                    _wallPosts.Add(post);
                }
                
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException)
                    await new MessageDialog("Error connecting to VK").ShowAsync();
                else
                    await new MessageDialog("Error loading groups").ShowAsync();
            }
            _inCall = false;
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
            _CurrentIndex = _wallPosts.IndexOf((VKWallPost)((e.OriginalSource as ListView).DataContext));
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

                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(() =>
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
