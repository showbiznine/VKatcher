using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using VKatcher.Services;
using VKatcherShared.Messages;
using VKatcherShared.Services;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace VKatcher.ViewModels
{
    public class NowPlayingPageViewModel : ViewModelBase
    {
        #region Fields
        public VKAudio CurrentTrack { get; set; }
        public ObservableCollection<object> CurrentPlaylist { get; set; }
        private DispatcherTimer _timer;
        private bool _sliderPressed;
        public string _trackTime { get; set; }
        public double _trackPosition { get; set; }
        #endregion

        #region Commands
        public RelayCommand PlayPauseCommand { get; set; }
        public RelayCommand SkipNextCommand { get; set; }
        public RelayCommand SkipPreviousCommand { get; set; }
        public RelayCommand<Grid> DownloadTrackCommand { get; private set; }
        public RelayCommand<Grid> DeleteDownloadCommand { get; private set; }
        public RelayCommand<Grid> AddToPlaylistCommand { get; private set; }
        public RelayCommand<Grid> PlayNextCommand { get; private set; }
        public RelayCommand<ItemClickEventArgs> PlaySongCommand { get; private set; }
        public RelayCommand<object> SongHoldingCommand { get; private set; }
        public RelayCommand<object> SongRightTappedCommand { get; private set; }
        public RelayCommand SliderDownCommand { get; set; }
        public RelayCommand SliderUpCommand { get; set; }
        #endregion

        public NowPlayingPageViewModel()
        {
            if (IsInDesignMode)
            {
                
            }
            else
            {
                CurrentTrack = new VKAudio();
                CurrentPlaylist = new ObservableCollection<object>();
                PlayerService.TrackChangedEvent += OnTrackChanged;
                InitializeCommands();
                SetupTimer();
            }
        }

        private void OnTrackChanged(VKAudio e)
        {
            CurrentTrack = e;
        }

        private void InitializeCommands()
        {
            PlayPauseCommand = new RelayCommand(() => PlayPause());
            SkipNextCommand = new RelayCommand(() => PlayerService.CurrentPlaybackList.MoveNext());
            SkipPreviousCommand = new RelayCommand(() => PlayerService.CurrentPlaybackList.MovePrevious());
            DownloadTrackCommand = new RelayCommand<Grid>(grid =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    var a = grid.DataContext as VKAudio;
                    DownloadTrack(a);
                });
            });
            DeleteDownloadCommand = new RelayCommand<Grid>(grid =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var att = grid.DataContext as VKAudio;
                    if (att.IsPlaying)
                    {
                        PlayerService.CurrentPlaybackList.MoveNext();
                    }
                    await FileService.DeleteDownload(att);
                    att.IsOffline = false;
                });
            });
            AddToPlaylistCommand = new RelayCommand<Grid>(grid =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    var att = grid.DataContext as VKAudio;
                    PlayerService.AddAudioToPlaylist(att);
                });
            });
            PlayNextCommand = new RelayCommand<Grid>(grid =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    var att = grid.DataContext as VKAudio;
                    PlayerService.PlayAudioNext(att);
                });
            });
            PlaySongCommand = new RelayCommand<ItemClickEventArgs>(args => OnSongListItemClick(args));
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
            SliderDownCommand = new RelayCommand(() =>
            {
                _sliderPressed = true;
            });
            SliderUpCommand = new RelayCommand(() =>
            {
                //_mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(_trackPosition);
                _sliderPressed = false;
            });
        }

        private void PlayPause()
        {
            //var state = _mediaPlayer.PlaybackSession.PlaybackState;
            //if (state == MediaPlaybackState.Playing)
            //{
            //    _mediaPlayer.Pause();
            //}
            //else if (state == MediaPlaybackState.Paused)
            //{
            //    _mediaPlayer.Play();
            //}
        }

        private async Task DownloadTrack(VKAudio a)
        {
            var file = await a.DownloadTrack();
            if (file != null)
            {
                FileService.WriteDownloads(a, file);
            }
        }

        public async Task OnSongListItemClick(ItemClickEventArgs e)
        {
            bool containsOffline = false;
            if (e.ClickedItem is VKAudio)
            {
                if (CurrentTrack != null)
                {
                    CurrentTrack.IsPlaying = false;
                }
                CurrentTrack = (e.ClickedItem as VKAudio);
                Debug.WriteLine("Clicked " + CurrentTrack.title);
                if (App.ViewModelLocator.Main._currentTrack != null)
                {
                    App.ViewModelLocator.Main._currentTrack.IsPlaying = false;
                }
                CurrentTrack.IsPlaying = true;
                App.ViewModelLocator.Main._currentTrack = CurrentTrack;

                if (containsOffline)
                {
                    await Task.Delay(500);
                }
            }
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
            //try
            //{
            //    if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing &&
            //        !_sliderPressed)
            //    {
            //        var ts = _mediaPlayer.PlaybackSession.Position;
            //        if (ts.Hours > 0)
            //        {
            //            _trackTime = _mediaPlayer.PlaybackSession.Position.ToString(@"hh\:mm\:ss");
            //        }
            //        else
            //        {
            //            _trackTime = _mediaPlayer.PlaybackSession.Position.ToString(@"mm\:ss");
            //        }
            //        _trackPosition = _mediaPlayer.PlaybackSession.Position.TotalSeconds;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine(ex.Message);
            //}
        }
        #endregion
    }
}
