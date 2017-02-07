using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK;
using VK.WindowsPhone.SDK.API.Model;
using VKatcher.Services;
using VKatcher.ViewModels;
using VKatcherShared;
using VKatcherShared.Messages;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VKatcher.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool isBackgroundTaskRunning = false;
        private bool IsBackgroundTaskRunning
        {
            get
            {
                if (isBackgroundTaskRunning)
                    return true;

                object value = ApplicationSettingsHelper.ReadResetSettingsValue("backgroundtaskstate");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    isBackgroundTaskRunning = ((string)value).Equals("BackgroundTaskRunning");
                    return isBackgroundTaskRunning;
                }
            }
        }
        const int RPC_S_SERVER_UNAVAILABLE = -2147023174; // 0x800706BA
        private AutoResetEvent backgroundAudioTaskStarted;
        public MediaPlayer _mediaPlayer;
        private List<string> _scope = new List<string> { VKScope.FRIENDS, VKScope.WALL, VKScope.PHOTOS, VKScope.AUDIO, VKScope.GROUPS };
        private bool _controlsOpen;
        private bool _isNowPlaying = false;
        private DispatcherTimer _sliderTimer;

        public MainPage()
        {
            this.InitializeComponent();
            DispatcherHelper.Initialize();
            #region Media Player
            backgroundAudioTaskStarted = new AutoResetEvent(false);
            _mediaPlayer = BackgroundMediaPlayer.Current;
            _mediaPlayer.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground; 
            #endregion
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
            myFrame.Navigated += OnNavigated;
            VKSDK.AccessTokenReceived += (sender, args) =>
            {
                App.ViewModelLocator.Main.Init();
            };
            btnSmart.Loaded += (s, e) =>
            {
                btnSmart.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.TranslateRailsX | ManipulationModes.TranslateRailsY; //| ManipulationModes.TranslateInertia;
            };
            SetupSliderTimer();
            #region App State
            Application.Current.Suspending += ForegroundApp_Suspending;
            Application.Current.Resuming += ForegroundApp_Resuming; 
            #endregion
        }

        private void SetupSliderTimer()
        {
            _sliderTimer = new DispatcherTimer();
            _sliderTimer.Interval = TimeSpan.FromSeconds(4);
            _sliderTimer.Tick += _sliderTimer_Tick;
        }

        private void _sliderTimer_Tick(object sender, object e)
        {
            CloseSlider.Begin();
            SmartHideTime.Begin();
            _sliderTimer.Stop();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ApplicationSettingsHelper.SaveSettingsValue("appstate", "Active");
            VKSDK.Initialize("5545387");
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                VKSDK.WakeUpSession();
            });
            if (VKSDK.IsLoggedIn)
            {
                (DataContext as MainViewModel).Init();
            }
            else
            {
                var test = await DataService.GetToken(null, null);
                //VKSDK.Authorize(_scope, false, false);
            }
        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = myFrame.CanGoBack ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;
            if (myFrame.Content is NowPlayingPage)
            {
                _isNowPlaying = true;
            }
            else
            {
                _isNowPlaying = false;
            }
            if (_controlsOpen && myFrame.Content is NowPlayingPage)
            {
                ClosePlaybackControls_Staggered.Begin();
                _controlsOpen = false;
            }
            else if (!_controlsOpen && 
                _mediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.None)
            {
                OpenPlaybackControls_Staggered.Begin();
                _controlsOpen = true;
            }

        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (myFrame.CanGoBack)
            {
                e.Handled = true;
                myFrame.GoBack();
            }
        }

        private void ForegroundApp_Resuming(object sender, object e)
        {
            ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.AppState, AppState.Active.ToString());

            // Verify the task is running
            if (isBackgroundTaskRunning)
            {
                // If yes, it's safe to reconnect to media play handlers
                //AddMediaPlayerEventHandlers();

                // Send message to background task that app is resumed so it can start sending notifications again
                MessageService.SendMessageToBackground(new AppResumedMessage());

            }

            else
            {

            }
        }

        private void ForegroundApp_Suspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // Only if the background task is already running would we do these, otherwise
            // it would trigger starting up the background task when trying to suspend.
            if (isBackgroundTaskRunning)
            {
                // Stop handling player events immediately
                //RemoveMediaPlayerEventHandlers();

                // Tell the background task the foreground is suspended
                MessageService.SendMessageToBackground(new AppSuspendedMessage());
            }
            // Persist that the foreground app is suspended
            ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.AppState, AppState.Suspended.ToString());

            deferral.Complete();
        }

        private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            switch (sender.PlaybackState)
            {
                case MediaPlaybackState.None:
                    DispatcherHelper.CheckBeginInvokeOnUI(() => ClosePlaybackControls_Staggered.Begin());
                    _controlsOpen = false;
                    break;
                case MediaPlaybackState.Playing:
                    if (!_controlsOpen && !_isNowPlaying)
                    {
                        DispatcherHelper.CheckBeginInvokeOnUI(() => OpenPlaybackControls_Staggered.Begin());
                        _controlsOpen = true; 
                    }
                    break;
                default:
                    break;
            }
        }

        #region BackgroundTask
        private void StartBackgroundAudioTask()
        {
            //AddMediaPlayerEventHandlers();
            var startResult = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                bool result = backgroundAudioTaskStarted.WaitOne(100);
                //Send message to initiate playback
                if (result == true)
                {
                    //MessageService.SendMessageToBackground(new UpdatePlaylistMessage(playlistView.Songs.ToList()));
                    //MessageService.SendMessageToBackground(new StartPlaybackMessage());
                }
                else
                {
                    throw new Exception("Background Audio Task didn't start in expected time");
                }
            });
            startResult.Completed = new AsyncActionCompletedHandler(BackgroundTaskInitializationCompleted);
        }

        private void BackgroundTaskInitializationCompleted(IAsyncAction action, AsyncStatus status)
        {
            if (status == AsyncStatus.Completed)
            {
                Debug.WriteLine("Background Audio Task initialized");
            }
            else if (status == AsyncStatus.Error)
            {
                Debug.WriteLine("Background Audio Task could not initialized due to an error ::" + action.ErrorCode.ToString());
            }

        }

        private void ResetAfterLostBackground()
        {
            BackgroundMediaPlayer.Shutdown();
            isBackgroundTaskRunning = false;
            backgroundAudioTaskStarted.Reset();
            ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.BackgroundTaskState, BackgroundTaskState.Unknown.ToString());

            try
            {
                BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
            }
            catch (Exception ex)
            {
                if (ex.HResult == RPC_S_SERVER_UNAVAILABLE)
                {
                    throw new Exception("Failed to get a MediaPlayer instance.");
                }
                else
                {
                    throw;
                }
            }
        }

        private void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            TrackChangedMessage trackChangedMessage;
            if (MessageService.TryParseMessage(e.Data, out trackChangedMessage))
            {
                int index;
                VKAudio track;
                var list = App.ViewModelLocator.Main._currentPlaylist.ToList();
                if (!trackChangedMessage.TrackURL.ToString().StartsWith("file"))
                {
                    index = list.FindIndex(i => (string)i.url == trackChangedMessage.TrackURL.ToString());
                }
                else
                {
                    index = list.FindIndex(i => (string)i.url == trackChangedMessage.TrackURL.LocalPath.ToString());
                }
                track = list[index];
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    App.ViewModelLocator.Main._currentTrack.IsPlaying = false;
                    if (App.ViewModelLocator.Feed._selectedTrack != null)
                    {
                        App.ViewModelLocator.Feed._selectedTrack.IsPlaying = false;
                    }
                    //if (App.ViewModelLocator.MyMusic._selectedTrack != null)
                    //{
                    //    App.ViewModelLocator.Feed._selectedTrack.IsPlaying = false;
                    //}
                    track.IsPlaying = true;
                    App.ViewModelLocator.Main._currentTrack = track;
                    App.ViewModelLocator.Feed._selectedTrack = track;
                });

                BackgroundMediaPlayer.Current.Play();
                return;
            }
        }
        #endregion

        private void btnSmart_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var man = e.Cumulative.Translation;
            if (man.Y < -20)
            {
                SmartSwipeUpSlide.Begin();
                OpenSlider.Begin();
                SmartShowTime.Begin();
                _sliderTimer.Start();
            }
            if (man.X > 20)
            {
                SmartSwipeRightSlide.Begin();
            }
            if (man.X < -20)
            {
                SmartSwipeLeftSlide.Begin();
            }
        }
    }
}
