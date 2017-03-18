using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK;
using VK.WindowsPhone.SDK.API.Model;
using VKatcher.Services;
using VKatcher.Views;
using VKatcherShared.Messages;
using Windows.ApplicationModel.Background;
using Windows.Media.Playback;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace VKatcher.ViewModels
{
    public class MainViewModel : ViewModelBase
    {

        #region Fields
        private DispatcherTimer _timer;
        private bool _sliderPressed;
        private MediaPlayer MediaPlayer;
        public string _trackTime { get; set; }
        public double _trackPosition { get; set; }
        public string _searchQuery { get; set; }
        public VKAudio _currentTrack { get; set; }
        public INavigationService _navigationService { get { return ServiceLocator.Current.GetInstance<INavigationService>(); } }
        public bool _isPlaying { get; set; }
        public bool IsMenuOpen { get; set; }
        #endregion

        #region Commands
        public RelayCommand GoToNowPlayingCommand { get; set; }
        public RelayCommand GoToMyMusicCommand { get; set; }
        public RelayCommand GoToSettingsCommand { get; set; }
        public RelayCommand PlayPauseCommand { get; set; }
        public RelayCommand SkipNextCommand { get; set; }
        public RelayCommand SkipPreviousCommand { get; set; }
        public RelayCommand SearchCommand { get; set; }
        public RelayCommand<ManipulationCompletedRoutedEventArgs> SmartSwipeCommand { get; set; }
        public RelayCommand SliderDownCommand { get; set; }
        public RelayCommand SliderUpCommand { get; set; }
        #endregion

        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                //Design time data
            }
            else
            {
                DispatcherHelper.Initialize();
                _isPlaying = false;
                InitializeCommands();

                #region Background Tasks
                RegisterBackgroundTasks();
                #endregion

                MediaPlayer = PlayerService.MediaPlayer;
                LoadRoamingPlaylist();
                SetupTimer();

                MediaPlayer.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
                PlayerService.CurrentPlaybackList.CurrentItemChanged += OnTrackChanged;
            }
        }

        private async Task RegisterBackgroundTasks()
        {
            var bgStatus = await BackgroundExecutionManager.RequestAccessAsync();
            await Task.Run(() =>
            {
                var checkTask = RegisterBackgroundTask("CheckNewPostsTask",
                    new TimeTrigger(15, false),
                    new SystemCondition(SystemConditionType.InternetAvailable));
            });
            await Task.Run(() =>
            {
                var dlTask = RegisterBackgroundTask("DownloadPostsTask",
                    new TimeTrigger(15, false), 
                    new SystemCondition(SystemConditionType.FreeNetworkAvailable));
            });
        }

        private void LoadRoamingPlaylist()
        {
            var pl = AppDataService.GetRoamingSetting("current_playlist") as string;
            if (pl != null)
            {
                var playlist = JsonConvert.DeserializeObject<ObservableCollection<VKAudio>>(pl);
                var currentTrack = AppDataService.GetRoamingSetting("current_track") as VKAudio;
                var index = currentTrack == null ? 0 : playlist.IndexOf(currentTrack);
                PlayerService.BuildPlaylistFromCollection(playlist, index, false);
            }
        }

        private void OnTrackChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            if (sender.CurrentItem != null && PlayerService.CurrentPlaylist.Count > 0)
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    _currentTrack = PlayerService.CurrentPlaylist[(int)sender.CurrentItemIndex];
                }); 
            }
        }

        public static async Task<BackgroundTaskRegistration> RegisterBackgroundTask(string name,
            IBackgroundTrigger trigger,
            IBackgroundCondition condition)
        {
            //Check for existing registrations
            foreach (var _task in BackgroundTaskRegistration.AllTasks)
            {
                if (_task.Value.Name == name)
                {
                    //The task is already registered

                    //_task.Value.Unregister(true);
                    return (BackgroundTaskRegistration)(_task.Value);
                }
            }

            //Register the task

            var builder = new BackgroundTaskBuilder();
            await BackgroundExecutionManager.RequestAccessAsync();
            builder.Name = name;
            builder.SetTrigger(trigger);

            if (condition != null)
            {
                builder.AddCondition(condition);
            }
            BackgroundTaskRegistration task = builder.Register();
            return task;
        }

        #region Timer
        private void SetupTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            StartTimer();
        }

        private void StartTimer()
        {
            _timer.Tick += _timer_Tick;
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer.Stop();
            _timer.Tick -= _timer_Tick;
        }

        private void _timer_Tick(object sender, object e)
        {
            try
            {
                if (MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing &&
                    !_sliderPressed)
                {
                    var ts = MediaPlayer.PlaybackSession.Position;
                    if (ts.Hours > 0)
                    {
                        _trackTime = MediaPlayer.PlaybackSession.Position.ToString(@"hh\:mm\:ss");
                    }
                    else
                    {
                        _trackTime = MediaPlayer.PlaybackSession.Position.ToString(@"mm\:ss");
                    }
                    _trackPosition = MediaPlayer.PlaybackSession.Position.TotalSeconds;
                }
            }
            catch (Exception)
            {
                StopTimer();
            }
        }
        #endregion

        private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            switch (sender.PlaybackState)
            {
                case MediaPlaybackState.None:
                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        _isPlaying = false;
                        App.ViewModelLocator.Settings.IsPlaying = false;
                    });
                    break;
                case MediaPlaybackState.Playing:
                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        _isPlaying = true;
                        App.ViewModelLocator.Settings.IsPlaying = true;
                    });
                    break;
                default:
                    break;
            }
        }

        private void InitializeCommands()
        {
            GoToNowPlayingCommand = new RelayCommand(() =>
            {
                //var lst = new ObservableCollection<object>(_currentPlaylist);
                //lst.Add(_currentTrack);
                //lst.Add(_currentPlaylist);
                //Debug.WriteLine(_currentTrack.photo_url);
                //App.ViewModelLocator.NowPlaying._currentPlaylist = lst;
                //App.ViewModelLocator.NowPlaying._currentTrack = _currentTrack;
                //_navigationService.NavigateTo(typeof(NowPlayingPage));
                IsMenuOpen = false;
            });
            GoToSettingsCommand = new RelayCommand(() =>
            {
                _navigationService.NavigateTo(typeof(SettingsPage));
                IsMenuOpen = false;
            });
            GoToMyMusicCommand = new RelayCommand(() =>
            {
                _navigationService.NavigateTo(typeof(MyMusicPage));
                IsMenuOpen = false;
            });
            PlayPauseCommand = new RelayCommand(() => PlayPause());
            SkipNextCommand = new RelayCommand(() => PlayerService.CurrentPlaybackList.MoveNext());
            SkipPreviousCommand = new RelayCommand(() => PlayerService.CurrentPlaybackList.MovePrevious());
            SmartSwipeCommand = new RelayCommand<ManipulationCompletedRoutedEventArgs>(e => OnSwipeCompleted(e));
            SliderDownCommand = new RelayCommand(() =>
            {
                _sliderPressed = true;
            });
            SliderUpCommand = new RelayCommand(() =>
            {
                MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(_trackPosition);
                _sliderPressed = false;
            });
            SearchCommand = new RelayCommand(() =>
            {
                _navigationService.NavigateTo(typeof(SearchPage));
                App.ViewModelLocator.Main.IsMenuOpen = false;
                App.ViewModelLocator.Search._searchQuery = _searchQuery;
                App.ViewModelLocator.Search.Search();
            });
        }

        private void OnSwipeCompleted(ManipulationCompletedRoutedEventArgs e)
        {
            var man = e.Cumulative.Translation;
            if (man.X > 20)
            {
                PlayerService.CurrentPlaybackList.MoveNext();
            }
            if (man.X < -20)
            {
                PlayerService.CurrentPlaybackList.MovePrevious();
            }
        }

        private void PlayPause()
        {
            var state = MediaPlayer.PlaybackSession.PlaybackState;
            if (state == MediaPlaybackState.Playing)
            {
                MediaPlayer.Pause();
            }
            else if (state == MediaPlaybackState.Paused)
            {
                MediaPlayer.Play();
            }
        }

        public void Init()
        {
            _navigationService.NavigateTo(typeof(CommunitiesPage));
        }
    }
}
