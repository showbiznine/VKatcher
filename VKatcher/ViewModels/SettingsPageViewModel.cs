using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK;
using VK.WindowsPhone.SDK.API;
using VK.WindowsPhone.SDK.API.Model;
using VK.WindowsPhone.SDK.Util;
using VKatcher.Services;
using VKatcher.Views;
using VKatcherShared.Services;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace VKatcher.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        public ObservableCollection<VKGroup> SubscribedGroups { get; set; }
        public ObservableCollection<VKTag> SubscribedTags { get; set; }
        //public ObservableCollection<Playlist> SpotifyPlaylists { get; set; }
        public VKUser MyUser { get; set; }
        public bool IsPlaying { get; set; }
        public INavigationService _navigationService { get { return ServiceLocator.Current.GetInstance<INavigationService>(); } }
        public bool IsSpotifyConnected { get; set; }

        public RelayCommand<ItemClickEventArgs> SelectGroup { get; set; }
        public RelayCommand<object> UnsubscribeCommand { get; set; }
        public RelayCommand LogOutCommand { get; set; }
        public RelayCommand ConnectSpotifyCommand { get; set; }
        public RelayCommand ClearTokenCommand { get; set; }
        public RelayCommand ClearToDownloadCommand { get; set; }

        public SettingsPageViewModel()
        {
            if (IsInDesignMode)
            {
                //SpotifyPlaylists = new ObservableCollection<Playlist>
                //{
                //    new Playlist {Name = "Test 1", Tracks = new Page<PlaylistTrack> { Total = 30} },
                //    new Playlist {Name = "Test 2", Tracks = new Page<PlaylistTrack> { Total = 15} },
                //    new Playlist {Name = "Test 3", Tracks = new Page<PlaylistTrack> { Total = 4} }
                //};
            }
            else
            {
                //SpotifyPlaylists = new ObservableCollection<Playlist>();
                IsPlaying = App.ViewModelLocator.Main._isPlaying;
                Initializesubscriptions();
                InitializeCommands();
                InitializeUser();
            }
        }

        private async void InitializeUser()
        {
            //await Task.Run(() =>
            //{
            //    VKRequest.Dispatch<List<VKUser>>(
            //        new VKRequestParameters(
            //            "users.get",
            //            "fields", "photo_200"),
            //        async (res) =>
            //        {
            //            if (res.ResultCode == VKResultCode.Succeeded)
            //            {
            //                await VKExecute.ExecuteOnUIThread(() =>
            //                {
            //                    MyUser = res.Data[0];
            //                });
            //            }
            //        });
            //});
        }

        private async void LoadSpotifyPlaylists()
        {
            //var pl = await SpotifyService.GetUserPlaylists();
            //foreach (var playlist in pl)
            //{
            //    SpotifyPlaylists.Add(playlist);
            //}
        }

        private void InitializeCommands()
        {
            SelectGroup = new RelayCommand<ItemClickEventArgs>(args =>
            {
                var group = args.ClickedItem as VKGroup;
                _navigationService.NavigateTo(typeof(FeedPage), group);
            });
            UnsubscribeCommand = new RelayCommand<object>(async obj =>
            {
                if (obj is VKGroup)
                {
                    MessageDialog msg = new MessageDialog("Are you sure you want to unsubscribe from " +
                        ((VKGroup)obj).name + "?");
                    msg.Commands.Add(new UICommand("Yes", async (command) =>
                    {
                        var res = await SubscriptionService.SubscribeToGroup((VKGroup)obj);
                        if (!res)
                        {
                            SubscribedGroups.Remove((VKGroup)obj);
                        }
                    }));
                    msg.Commands.Add(new UICommand("No"));
                    msg.DefaultCommandIndex = 1;
                    await msg.ShowAsync();
                }
                else if (obj is VKTag)
                {
                    MessageDialog msg = new MessageDialog("Are you sure you want to unsubscribe from " +
                        ((VKTag)obj).tag + "?");
                    msg.Commands.Add(new UICommand("Yes", async (command) =>
                    {
                        var res = await SubscriptionService.SubscribeToTag((VKTag)obj);
                        if (!res)
                        {
                            SubscribedTags.Remove((VKTag)obj);
                        }
                    }));
                    msg.Commands.Add(new UICommand("No"));
                    msg.DefaultCommandIndex = 1;
                    await msg.ShowAsync();
                }

            });
            LogOutCommand = new RelayCommand(() =>
            {
                VKSDK.Logout();
                _navigationService.NavigateTo(typeof(MainPage));
            });
            ConnectSpotifyCommand = new RelayCommand(async () =>
            {
                await AuthenticateSpotify();
                //LoadSpotifyPlaylists();
            });
            ClearTokenCommand = new RelayCommand(() =>
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["token"] = null;
            });
            ClearToDownloadCommand = new RelayCommand(() =>
            {

            });
        }

        private async Task AuthenticateSpotify()
        {
            //var res = await SpotifyService.GetAuthCode();
            //if (res)
            //{
            //    LoadSpotifyPlaylists();
            //}

            //IsSpotifyConnected = res;
        }

        public async void Initializesubscriptions()
        {
            SubscribedGroups = new ObservableCollection<VKGroup>();
            SubscribedGroups = await SubscriptionService.LoadSubscribedGroups();

            SubscribedTags = new ObservableCollection<VKTag>();
            SubscribedTags = await SubscriptionService.LoadSubscribedTags();
        }
    }
}
