using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK;
using VK.WindowsPhone.SDK.API.Model;
using VKatcher.Services;
using VKatcher.ViewModels;
using VKatcherShared;
using VKatcherShared.Messages;
using VKatcherShared.Services;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Popups;
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
        private List<string> _scope = new List<string> { VKScope.FRIENDS, VKScope.WALL, VKScope.PHOTOS, VKScope.AUDIO, VKScope.GROUPS };
        private bool _controlsOpen;
        private bool _isNowPlaying = false;
        private DispatcherTimer _sliderTimer;

        public MainPage()
        {
            this.InitializeComponent();
            DispatcherHelper.Initialize();
            #region Media Player
            PlayerService.MediaPlayer.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
            #endregion
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
            myFrame.Navigated += OnNavigated;
            btnSmart.Loaded += (s, e) =>
            {
                btnSmart.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.TranslateRailsX | ManipulationModes.TranslateRailsY; //| ManipulationModes.TranslateInertia;
            };
            SetupSliderTimer();
        }

        #region Timer
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
        #endregion

        #region Navigation
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ApplicationSettingsHelper.SaveSettingsValue("appstate", "Active");
            try
            {
                await OneDriveService.InitializeAsync();
                var loggedIn = AuthenticationService.CheckLoggedIn("VK", "test");
                if (loggedIn)
                    (DataContext as MainViewModel).Init();
                else
                {
                    await AuthenticationService.VKLogin();
                    (DataContext as MainViewModel).Init();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                if (ex is HttpRequestException)
                {
                    await new MessageDialog("Error connecting to the internet").ShowAsync();
                }
                else
                {
                    await new MessageDialog("Error connecting to VK").ShowAsync();
                }
            }

            if (e.Parameter is string)
            {
                switch ((string)e.Parameter)
                {
                    case "downloads":
                        myFrame.Navigate(typeof(MyMusicPage));
                        break;
                    default:
                        break;
                }
            }
            //var test = await AuthenticationService.VKLogin();
            //VKSDK.Authorize(_scope, false, false);
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
                PlayerService.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
                PlayerService.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
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
        #endregion

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

        private void navTest_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //navTest.IsSelected = !navTest.IsSelected;
        }
    }
}
