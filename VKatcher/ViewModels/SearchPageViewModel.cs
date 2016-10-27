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

namespace VKatcher.ViewModels
{
    public class SearchPageViewModel : ViewModelBase
    {
        #region Commands
        public RelayCommand<Visibility> SearchCommand { get; set; }
        public RelayCommand<ItemClickEventArgs> PlaySongCommand { get; set; }
        public RelayCommand<VKGroup> SelectGroup { get; set; }
        public RelayCommand<object> SongHoldingCommand { get; private set; }
        #endregion

        #region Fields
        public MainViewModel _mainVM;
        public VKAudio _selectedTrack;
        public ObservableCollection<VKAudio> _currentPlaylist;
        public string _searchQuery { get; set; }
        public string _resultsString { get; set; }
        public ObservableCollection<VKGroup> GroupResults { get; set; }
        public ObservableCollection<VKAudio> TrackResults { get; set; }
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
                InitializeCommands();
            }
        }

        private void InitializeCommands()
        {
            SearchCommand = new RelayCommand<Visibility>(async vis =>
            {
                try
                {
                    if (vis == Visibility.Visible &&
                !string.IsNullOrWhiteSpace(_searchQuery))
                    {
                        DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                        {
                            GroupResults.Clear();
                            GroupResults = await DataService.SearchGroups(_searchQuery);
                            TrackResults.Clear();
                            TrackResults = await DataService.SearchAudio(_searchQuery);
                            _resultsString = "\"" + _searchQuery + "\"";
                        });
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
                        _currentPlaylist = new ObservableCollection<VKAudio>();
                        foreach (var item in lst.Items)
                        {
                            _currentPlaylist.Add((item as VKAudio));
                        }
                        App.ViewModelLocator.Main._currentPlaylist = _currentPlaylist;
                    });

                    MessageService.SendMessageToBackground(new UpdatePlaylistMessage(App.ViewModelLocator.Main._currentPlaylist));
                    MessageService.SendMessageToBackground(new TrackChangedMessage(new Uri(_selectedTrack.url)));
                    MessageService.SendMessageToBackground(new StartPlaybackMessage());
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
        }
    }
}
