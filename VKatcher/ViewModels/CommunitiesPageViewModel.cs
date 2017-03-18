using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using VKatcher.Services;
using VKatcher.Views;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace VKatcher.ViewModels
{
    public class CommunitiesPageViewModel : ViewModelBase
    {
        #region Fields

        public ObservableCollection<VKGroup> _results { get; set; }
        public string _searchQuery { get; set; }
        public Frame _frame;
        public bool _inCall { get; set; }
        public INavigationService _navigationService { get { return ServiceLocator.Current.GetInstance<INavigationService>(); } }
        #endregion

        #region Commands
        public RelayCommand SearchCommand { get; set; }
        public RelayCommand OpenMenuCommand { get; set; }
        public RelayCommand GoToSettingsCommand { get; set; }
        public RelayCommand GoToMyMusicCommand { get; set; }
        public RelayCommand<ItemClickEventArgs> SelectGroupCommand { get; set; }
        #endregion

        public CommunitiesPageViewModel()
        {
            if (IsInDesignMode)
            {

            }
            else
            {
                DispatcherHelper.Initialize();
                InitializeCommands();
                if (_results == null)
                {
                    _results = new ObservableCollection<VKGroup>();
                    LoadGroups();
                }
            }
        }

        private void InitializeCommands()
        {
            SearchCommand = new RelayCommand(() => 
            {
                _navigationService.NavigateTo(typeof(SearchPage));
            });
            GoToSettingsCommand = new RelayCommand(() =>
            {
                //SpotifyService.Authenticate();
                //_navigationService.NavigateTo(typeof(SettingsPage));
            });
            GoToMyMusicCommand = new RelayCommand(() =>
            {
                _navigationService.NavigateTo(typeof(MyMusicPage));
            });
            OpenMenuCommand = new RelayCommand(() =>
            {
                App.ViewModelLocator.Main.IsMenuOpen = !App.ViewModelLocator.Main.IsMenuOpen;
            });
            SelectGroupCommand = new RelayCommand<ItemClickEventArgs>(item =>
            {
                var g = item.ClickedItem as VKGroup;
                App.ViewModelLocator.Feed._currentGroup = g;
                App.ViewModelLocator.Feed.LoadPosts(0, 30, true);
                _navigationService.NavigateTo(typeof(FeedPage));
            });
        }

        private async Task LoadGroups()
        {
            _inCall = true;
            try
            {
                _results.Clear();
                _results = await DataService.LoadMyGroups();
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
    }
}
