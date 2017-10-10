using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Practices.ServiceLocation;
using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
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
using VKatcher.ContentDialogs;
using VKatcher.Services;
using VKatcher.Views;
using VKatcherShared.Messages;
using VKatcherShared.Services;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;
using Windows.System.RemoteSystems;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Threading;

namespace VKatcher.ViewModels
{
    public class FeedPageViewModel : ViewModelBase
    {
        #region Fields
        public VKGroup CurrentGroup { get; set; }
        public IncrementalLoadingCollection<WallPostCollection,VKWallPost> WallPosts { get; set; }
        public int _offset { get; set; }
        public bool _inCall { get; set; }
        private VKWallPost _heldWallPost;
        private int _heldAudioIndex;
        public INavigationService _navigationService { get { return ServiceLocator.Current.GetInstance<INavigationService>(); } }


        //public static VKAudio _selectedTrack;
        //public static ObservableCollection<VKAudio> _currentPlaylist;
        //private const string _downloadedDB = "downloaded_files.json";
        #endregion

        #region Commands
        public RelayCommand ToggleMenuCommand { get; private set; }
        public RelayCommand LoadPostsCommand { get; private set; }
        public RelayCommand RefreshPostsCommand { get; private set; }
        public RelayCommand SubscribeCommand { get; private set; }
        public RelayCommand<Grid> UploadToOneDriveCommand { get; private set; }
        public RelayCommand<Grid> DeleteDownloadCommand { get; private set; }
        public RelayCommand<Grid> AddToPlaylistCommand { get; private set; }
        public RelayCommand<Grid> PlayNextCommand { get; private set; }
        public RelayCommand<Grid> PlayOnRemoteDeviceCommand { get; private set; }
        public RelayCommand<ItemClickEventArgs> SongListViewItemClickCommand { get; private set; }
        public RelayCommand<LinkClickedEventArgs> TagClickCommand { get; private set; }
        public RelayCommand<object> SongHoldingCommand { get; private set; }
        public RelayCommand<Grid> DownloadTrackCommand { get; private set; }
        public RelayCommand<object> SongRightTappedCommand { get; private set; }
        #endregion

        public FeedPageViewModel()
        {
            if (IsInDesignMode)
            {
                CurrentGroup = new VKGroup()
                {
                    name = "Test Group",
                    IsSubscribed = true
                };
            }
            else
            {
                InitializeCommands();
                WallPosts = new IncrementalLoadingCollection<WallPostCollection, VKWallPost>();
            }
        }

        private void InitializeCommands()
        {
            ToggleMenuCommand = new RelayCommand(() => App.ViewModelLocator.Main.IsMenuOpen = !App.ViewModelLocator.Main.IsMenuOpen);
            LoadPostsCommand = new RelayCommand(async () => await LoadPosts(_offset, 30, true));
            RefreshPostsCommand = new RelayCommand(async () => await LoadPosts(_offset, 30, true));
            UploadToOneDriveCommand = new RelayCommand<Grid>(grid =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var att = grid.DataContext as VKAttachment;
                    await OneDriveService.SaveToOneDrive(att.audio);
                    await (new MessageDialog(string.Format("Uploaded {0} by {1} to OneDrive", att.audio.title, att.audio.artist))).ShowAsync();
                });
            });
            DeleteDownloadCommand = new RelayCommand<Grid>(grid =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var att = grid.DataContext as VKAttachment;
                    if (att.audio.IsPlaying)
                    {
                        PlayerService.CurrentPlaybackList.MoveNext();
                        await FileService.DeleteDownload(att.audio);
                        att.audio.IsOffline = false;
                    }
                });
            });
            AddToPlaylistCommand = new RelayCommand<Grid>(grid =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var att = grid.DataContext as VKAttachment;
                    if (!string.IsNullOrWhiteSpace(att.audio.url))
                    {
                        PlayerService.AddAudioToPlaylist(att.audio);
                    }
                    else
                        await new MessageDialog("This track has been deleted :(").ShowAsync();
                });
            });
            PlayNextCommand = new RelayCommand<Grid>(grid =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var att = grid.DataContext as VKAttachment;
                    if (!string.IsNullOrWhiteSpace(att.audio.url))
                    {
                        PlayerService.PlayAudioNext(att.audio);
                    }
                    else
                        await new MessageDialog("This track has been deleted :(").ShowAsync();
                });
            });
            PlayOnRemoteDeviceCommand = new RelayCommand<Grid>(grid =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var att = grid.DataContext as VKAttachment;
                    if (!string.IsNullOrWhiteSpace(att.audio.url))
                    {
                        var rdd = new RemoteDeviceDialog();
                        await rdd.ShowAsync();
                        if (rdd.SelectedRemoteDevice != null)
                        {
                            var audios = new ObservableCollection<VKAudio>();
                            foreach (var item in _heldWallPost.attachments)
                            {
                                audios.Add(item.audio);
                            }
                            await RemoteSystemService.PlayAudioOnRemoteDeviceAsync(audios, rdd.SelectedRemoteDevice, _heldAudioIndex);
                        }
                    }
                    else
                        await new MessageDialog("This track has been deleted :(").ShowAsync();
                });
            });

            SongListViewItemClickCommand = new RelayCommand<ItemClickEventArgs>(async args =>
            {
                await OnSongListItemClick(args);
            });
            DownloadTrackCommand = new RelayCommand<Grid>(async args =>
            {
                var attachment = args.DataContext as VKAttachment;
                await DownloadTrack(attachment.audio);
            });
            TagClickCommand = new RelayCommand<LinkClickedEventArgs>(args =>
            {
                OnTagClick(args.Link);
            });
            SubscribeCommand = new RelayCommand(async () =>
            {
                var sub = await SubscriptionService.SubscribeToGroup(CurrentGroup);
            });
            SongHoldingCommand = new RelayCommand<object>(sender =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    Grid obj = (Grid)sender;
                    var lst = obj.FindVisualAscendant<ListView>();
                    _heldWallPost = lst.DataContext as VKWallPost;
                    _heldAudioIndex = lst.Items.IndexOf(obj.DataContext);
                    FlyoutBase.ShowAttachedFlyout(obj);
                });
            });
            SongRightTappedCommand = new RelayCommand<object>(sender =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    Grid obj = (Grid)sender;
                    var lst = obj.FindVisualAscendant<ListView>();
                    _heldWallPost = lst.DataContext as VKWallPost;
                    _heldAudioIndex = lst.Items.IndexOf(obj.DataContext);
                    FlyoutBase.ShowAttachedFlyout(obj);
                });
            });
        }

        private void OnTagClick(string tag)
        {
            App.ViewModelLocator.Search.Search(tag, CurrentGroup.screen_name);
            _navigationService.NavigateTo(typeof(SearchPage));
        }

        public async Task LoadPosts(int offset, int count, bool clear)
        {
            _inCall = true;
            try
            {
                if (WallPosts != null && clear)
                {
                    WallPosts.Clear();
                }
                foreach (var post in await DataService.LoadWallPosts(CurrentGroup.id, offset, count))
                {
                    WallPosts.Add(post);
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

        private async Task DownloadTrack(VKAudio track)
        {
            var file = await track.DownloadTrack();
            if (file != null)
            {
                await FileService.WriteDownloads(track, file);
            }
        }

        public async Task OnSongListItemClick(ItemClickEventArgs e)
        {
            bool containsOffline = false;
            var clickedIndex = WallPosts.IndexOf((VKWallPost)((e.OriginalSource as ListView).DataContext));
            VKAudio selectedTrack;
            if (e.ClickedItem is VKAttachment)
            {
                selectedTrack = (e.ClickedItem as VKAttachment).audio;
                Debug.WriteLine("Clicked " + selectedTrack.title);
                if (App.ViewModelLocator.Main._currentTrack != null)
                {
                    App.ViewModelLocator.Main._currentTrack.IsPlaying = false;
                }
                selectedTrack.IsPlaying = true;

                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    ListView lst = e.OriginalSource as ListView;
                    int itemIndex = lst.Items.IndexOf(e.ClickedItem);
                    var postPlaylist = new ObservableCollection<VKAudio>();
                    foreach (var item in lst.Items)
                    {
                        if ((item as VKAttachment).audio.IsOffline)
                        {
                            containsOffline = true;
                        }
                        postPlaylist.Add((item as VKAttachment).audio);
                    }
                    PlayerService.BuildPlaylistFromCollection(postPlaylist, itemIndex, true);
                });

                if (containsOffline)
                {
                    await Task.Delay(500); 
                }
            }
        }
    }

    public class WallPostCollection : IIncrementalSource<VKWallPost>
    {
        public async Task<IEnumerable<VKWallPost>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default(CancellationToken))
        {
            var res = new List<VKWallPost>();
            foreach (var item in await DataService.LoadWallPosts(App.ViewModelLocator.Feed.CurrentGroup.id, pageIndex * pageSize, pageSize))
                res.Add(item);
            return res;
        }
    }
}
