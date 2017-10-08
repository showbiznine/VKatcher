using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using VKatcher.ContentDialogs;
using VKatcher.Services;
using VKatcher.Views;
using VKatcherShared.Messages;
using VKatcherShared.Services;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace VKatcher.ViewModels
{
    public class MyMusicPageViewModel : ViewModelBase
    {
        #region Fields
        public ObservableCollection<VKAudio> MySavedTracks { get; set; }
        public ObservableCollection<VKAudio> MyDownloads { get; set; }
        public ObservableCollection<VKAudio> MySuggestedMusic { get; set; }
        public static VKAudio SelectedTrack { get; set; }
        public bool InCall { get; set; }
        #endregion

        public RelayCommand<ItemClickEventArgs> PlaySongCommand { get; set; }
        public RelayCommand<Grid> DownloadTrackCommand { get; private set; }
        public RelayCommand<Grid> UploadToOneDriveCommand { get; private set; }
        public RelayCommand<Grid> DeleteDownloadCommand { get; private set; }
        public RelayCommand<object> SongHoldingCommand { get; private set; }
        public RelayCommand<object> SongRightTappedCommand { get; private set; }
        public RelayCommand<Grid> PlayOnRemoteDeviceCommand { get; private set; }

        public MyMusicPageViewModel()
        {
            if (IsInDesignMode)
            {
                #region Design Time Data
                MyDownloads = new ObservableCollection<VKAudio>();
                MyDownloads.Add(new VKAudio
                {
                    title = "Test Track 1",
                    artist = "Artist",
                    duration = 300
                });
                MyDownloads.Add(new VKAudio
                {
                    title = "Test Track 2",
                    artist = "Artist",
                    duration = 369
                });

                MySavedTracks = new ObservableCollection<VKAudio>();
                MySavedTracks.Add(new VKAudio
                {
                    title = "Test Track 3",
                    artist = "Artist",
                    duration = 600
                });
                MySavedTracks.Add(new VKAudio
                {
                    title = "Test Track 4",
                    artist = "Artist",
                    duration = 690
                }); 
                #endregion
            }
            else
            {
                DispatcherHelper.Initialize();
                InitializeCommands();
                LoadMyTracks();
                LoadMyDownloads();
                LoadMySuggestedMusicAsync();
            }
        }

        private void InitializeCommands()
        {
            PlaySongCommand = new RelayCommand<ItemClickEventArgs>(async e =>
            {
                var containsOffline = false;
                if (e.ClickedItem is VKAudio)
                {
                    if (SelectedTrack != null)
                    {
                        SelectedTrack.IsPlaying = false;
                    }
                    SelectedTrack = (e.ClickedItem as VKAudio);
                    Debug.WriteLine("Clicked " + SelectedTrack.title);
                    if (App.ViewModelLocator.Main._currentTrack != null)
                    {
                        App.ViewModelLocator.Main._currentTrack.IsPlaying = false;
                    }
                    SelectedTrack.IsPlaying = true;

                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        ListView lst = e.OriginalSource as ListView;
                        var itemIndex = lst.Items.IndexOf(e.ClickedItem);
                        var myMusicPlaylist = new ObservableCollection<VKAudio>();
                        foreach (var item in lst.Items)
                        {
                            if ((item as VKAudio).IsOffline)
                            {
                                containsOffline = true;
                            }
                            myMusicPlaylist.Add((item as VKAudio));
                        }
                        PlayerService.BuildPlaylistFromCollection(myMusicPlaylist, itemIndex, true);
                    });
                    if (containsOffline)
                    {
                        await Task.Delay(500);
                    }
                }
            });
            UploadToOneDriveCommand = new RelayCommand<Grid>(grid =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var audio = grid.DataContext as VKAudio;
                    await OneDriveService.SaveToOneDrive(audio);
                });
            });
            DownloadTrackCommand = new RelayCommand<Grid>(grid =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var att = grid.DataContext as VKAudio;
                    var file = await att.DownloadTrack();
                    var r = await OneDriveService.UploadFile(file, att.title + " - " + att.artist);
                    await (new MessageDialog(string.Format("Uploaded {0} by {1} to OneDrive", att.title, att.artist))).ShowAsync();
                });
            });
            DeleteDownloadCommand = new RelayCommand<Grid>(grid =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var att = grid.DataContext;
                    if (att is VKAttachment)
                    {
                        ((VKAttachment)att).audio.IsOffline = false;
                        await FileService.DeleteDownload(((VKAttachment)att).audio); 
                    }
                    else if (att is VKAudio)
                    {
                        await FileService.DeleteDownload((VKAudio)att);
                    }
                });
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
            PlayOnRemoteDeviceCommand = new RelayCommand<Grid>(grid =>
            {
                GalaSoft.MvvmLight.Threading.DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    var att = grid.DataContext as VKAudio;
                    if (!string.IsNullOrWhiteSpace(att.url))
                    {
                        var rdd = new RemoteDeviceDialog();
                        await rdd.ShowAsync();
                        if (rdd.SelectedRemoteDevice != null)
                        {
                            await RemoteSystemService.PlayAudioOnRemoteDeviceAsync(att, rdd.SelectedRemoteDevice);
                        }
                    }
                    else
                        await new MessageDialog("This track has been deleted :(").ShowAsync();
                });
            });
        }

        private async Task DownloadTrack(VKAudio track)
        {
            var file = await track.DownloadTrack();
            if (file != null)
            {
                await FileService.WriteDownloads(track, file);
            }
        }

        public async void LoadMyDownloads()
        {
            try
            {
                MyDownloads = await FileService.GetDownloads();
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException)
                    await new MessageDialog("Error connecting to VK").ShowAsync();
                else
                    await new MessageDialog("Error loading groups").ShowAsync();
            }
        }

        private async void LoadMySuggestedMusicAsync()
        {
            InCall = true;
            try
            {
                if (MySuggestedMusic == null)
                {
                    MySuggestedMusic = new ObservableCollection<VKAudio>();
                    var temp = await DataService.GetMyRadio();
                    foreach (var item in temp.response.items)
                    {
                        MySuggestedMusic.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException)
                    await new MessageDialog("Error connecting to VK").ShowAsync();
                else
                    await new MessageDialog("Error loading groups").ShowAsync();
            }
            InCall = false;
        }

        public async void LoadMyTracks()
        {
            InCall = true;
            if (await AuthenticationService.RefreshAccessToken())
            {
                try
                {
                    if (MySavedTracks == null)
                    {
                        MySavedTracks = new ObservableCollection<VKAudio>();
                        var temp = await DataService.LoadMyAudio();
                        foreach (var item in temp)
                        {
                            if (!string.IsNullOrWhiteSpace(item.url))
                                MySavedTracks.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is HttpRequestException)
                        await new MessageDialog("Error connecting to VK").ShowAsync();
                    else
                        await new MessageDialog("Error loading groups").ShowAsync();
                }
            }
            InCall = false;
        }
    }
}
