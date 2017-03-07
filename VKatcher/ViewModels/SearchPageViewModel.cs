using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Command;
using VKatcher.Services;
using System.Collections.ObjectModel;
using VK.WindowsPhone.SDK.API.Model;
using GalaSoft.MvvmLight.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using VKatcher.Views;
using VKatcherShared.Messages;
using Microsoft.Practices.ServiceLocation;
using Windows.UI.Xaml.Controls.Primitives;
using System.Net.Http;
using Windows.UI.Popups;
using VKatcherShared.Services;

namespace VKatcher.ViewModels
{
    public class SearchPageViewModel : ViewModelBase
    {
        #region Commands
        public RelayCommand<Visibility> SearchCommand { get; set; }
        public RelayCommand<ItemClickEventArgs> PlaySongCommand { get; set; }
        public RelayCommand<VKGroup> SelectGroup { get; set; }
        public RelayCommand<object> SongHoldingCommand { get; set; }
        public RelayCommand<ItemClickEventArgs> SongListViewItemClickCommand { get; set; }
        public RelayCommand SubscribeToTagCommand { get; set; }
        #endregion

        #region Fields
        public MainViewModel _mainVM;
        public VKAudio _selectedTrack;
        public ObservableCollection<VKAudio> _currentPlaylist;
        public string _searchQuery { get; set; }
        public VKTag TagQuery { get; set; }
        public string _resultsString { get; set; }
        public ObservableCollection<VKGroup> GroupResults { get; set; }
        public ObservableCollection<VKAudio> TrackResults { get; set; }
        public ObservableCollection<VKWallPost> PostResults { get; set; }
        public bool TracksVisible { get; set; }
        public bool PostsVisible { get; set; }
        public bool GroupsVisible { get; set; }
        public INavigationService _navigationService { get { return ServiceLocator.Current.GetInstance<INavigationService>(); } }
        #endregion

        public SearchPageViewModel()
        {
            if (IsInDesignMode)
            {

            }
            else
            {
                _mainVM = ((Window.Current.Content as Frame).Content as MainPage).DataContext as MainViewModel;
                GroupResults = new ObservableCollection<VKGroup>();
                TrackResults = new ObservableCollection<VKAudio>();
                PostResults = new ObservableCollection<VKWallPost>();
                InitializeCommands();
            }
        }

        private void InitializeCommands()
        {
            SearchCommand = new RelayCommand<Visibility>(async vis =>
            {
                try
                {
                    if ( //vis == Visibility.Visible &&
                !string.IsNullOrWhiteSpace(_searchQuery))
                    {
                        Search();
                    };
                }
                catch (Exception ex)
                {
                    if (ex is HttpRequestException)
                        await new MessageDialog("Error connecting to VK").ShowAsync();
                    else
                        await new MessageDialog("Error loading groups").ShowAsync();
                }
            });
            PlaySongCommand = new RelayCommand<ItemClickEventArgs>(e =>
            {
                if (e.ClickedItem is VKAudio)
                {
                    if (_selectedTrack != null)
                    {
                        _selectedTrack.IsPlaying = false;
                    }
                    _selectedTrack = (e.ClickedItem as VKAudio);
                    Debug.WriteLine("Clicked " + _selectedTrack.title);
                    if (_mainVM._currentTrack != null)
                    {
                        _mainVM._currentTrack.IsPlaying = false;
                    }
                    _selectedTrack.IsPlaying = true;
                    _mainVM._currentTrack = _selectedTrack;

                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        ListView lst = e.OriginalSource as ListView;
                        var itemIndex = lst.Items.IndexOf(e.ClickedItem);
                        var playlist = new ObservableCollection<VKAudio>();
                        foreach (var item in lst.Items)
                        {
                            _currentPlaylist.Add((item as VKAudio));
                        }
                        PlayerService.BuildPlaylistFromCollection(playlist, itemIndex, true);
                    });
                }
            });
            SelectGroup = new RelayCommand<VKGroup>(group =>
            {
                _navigationService.NavigateTo(typeof(FeedPage), group);
            });
            SongHoldingCommand = new RelayCommand<object>(sender =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    Grid obj = (Grid)sender;
                    FlyoutBase.ShowAttachedFlyout(obj);
                });
            });
            SongListViewItemClickCommand = new RelayCommand<ItemClickEventArgs>(args =>
            {
                OnSongListItemClick(args);
            });
            SubscribeToTagCommand = new RelayCommand(async () =>
            {
                if (await SubscriptionService.SubscribeToTag(TagQuery))
                    await new MessageDialog("Subscribed to tag").ShowAsync();
                else
                    await new MessageDialog("Failed to subscribe to tag").ShowAsync();

            });
        }

        public async void OnSongListItemClick(ItemClickEventArgs e)
        {
            bool containsOffline = false;
            //var clickedIndex = WallPosts.IndexOf((VKWallPost)((e.OriginalSource as ListView).DataContext));
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

        public void Search()
        {
            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                GroupResults.Clear();
                GroupResults = await DataService.SearchGroups(_searchQuery);
                TrackResults.Clear();
                TrackResults = await DataService.SearchAudio(_searchQuery);
                _resultsString = "\"" + _searchQuery + "\"";
            });
        }

        public void Search(string Tag, string groupScreenName)
        {
            var split = Tag.Split('#', '@');
            List<string> fixedSplit = new List<string>();
            foreach (var t in split)
            {
                if (!string.IsNullOrWhiteSpace(t))
                    fixedSplit.Add(t);
            }
            TagQuery = new VKTag
            {
                tag = fixedSplit[0],
                domain = fixedSplit.Count > 1 ? fixedSplit[1] : groupScreenName
            };
            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                //_inCall = true;
                TrackResults.Clear();               
                foreach (var post in await DataService.SearchWallByTag(TagQuery.tag, TagQuery.domain, 30))
                {
                    PostResults.Add(post);
                }
                TracksVisible = false;
                GroupsVisible = false;
                PostsVisible = true;
                //_inCall = false;
            });
        }
    }
}
