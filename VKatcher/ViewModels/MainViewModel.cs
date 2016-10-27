using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Practices.ServiceLocation;
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
        private MediaPlayer _mediaPlayer;
        private DispatcherTimer _timer;
        private bool _sliderPressed;
        public string _trackTime { get; set; }
        public double _trackPosition { get; set; }

        public VKAudio _currentTrack { get; set; }
        public ObservableCollection<VKAudio> _currentPlaylist { get; set; }
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
                var task = RegisterBackgroundTask("VKBackground.CheckNewPostsTask",
                    "CheckNewPostsTask",
                    new MaintenanceTrigger(30, false),
                    null);
                SetupTimer();
                _mediaPlayer = BackgroundMediaPlayer.Current;
                _mediaPlayer.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
            }
        }

        public static async Task<BackgroundTaskRegistration> RegisterBackgroundTask(string taskEntryPoint,
                                                        string name,
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
            builder.TaskEntryPoint = taskEntryPoint;
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
                if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing &&
                    !_sliderPressed)
                {
                    var ts = _mediaPlayer.PlaybackSession.Position;
                    if (ts.Hours > 0)
                    {
                        _trackTime = _mediaPlayer.PlaybackSession.Position.ToString(@"hh\:mm\:ss");
                    }
                    else
                    {
                        _trackTime = _mediaPlayer.PlaybackSession.Position.ToString(@"mm\:ss");
                    }
                    _trackPosition = _mediaPlayer.PlaybackSession.Position.TotalSeconds;
                }
            }
            catch (Exception)
            {
                throw;
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
                var lst = new ObservableCollection<object>(_currentPlaylist);
                //lst.Add(_currentTrack);
                //lst.Add(_currentPlaylist);
                Debug.WriteLine(_currentTrack.photo_url);
                App.ViewModelLocator.NowPlaying._currentPlaylist = lst;
                App.ViewModelLocator.NowPlaying._currentTrack = _currentTrack;
                _navigationService.NavigateTo(typeof(NowPlayingPage));
            });
            GoToSettingsCommand = new RelayCommand(() =>
            {
                _navigationService.NavigateTo(typeof(SettingsPage));
            });
            GoToMyMusicCommand = new RelayCommand(() =>
            {
                _navigationService.NavigateTo(typeof(MyMusicPage));
            });
            PlayPauseCommand = new RelayCommand(() => PlayPause());
            SkipNextCommand = new RelayCommand(() => MessageService.SendMessageToBackground(new SkipNextMessage()));
            SkipPreviousCommand = new RelayCommand(() => MessageService.SendMessageToBackground(new SkipPreviousMessage()));
            SmartSwipeCommand = new RelayCommand<ManipulationCompletedRoutedEventArgs>(e => OnSwipeCompleted(e));
            SliderDownCommand = new RelayCommand(() =>
            {
                _sliderPressed = true;
            });
            SliderUpCommand = new RelayCommand(() =>
            {
                _mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(_trackPosition);
                _sliderPressed = false;
            });
        }

        private void OnSwipeCompleted(ManipulationCompletedRoutedEventArgs e)
        {
            var man = e.Cumulative.Translation;
            if (man.X > 20)
            {
                MessageService.SendMessageToBackground(new SkipNextMessage());
            }
            if (man.X < -20)
            {
                MessageService.SendMessageToBackground(new SkipPreviousMessage());
            }
        }

        private void PlayPause()
        {
            var state = _mediaPlayer.PlaybackSession.PlaybackState;
            if (state == MediaPlaybackState.Playing)
            {
                _mediaPlayer.Pause();
            }
            else if (state == MediaPlaybackState.Paused)
            {
                _mediaPlayer.Play();
            }
        }

        public void Init()
        {
            _navigationService.NavigateTo(typeof(CommunitiesPage));
        }
    }
}
