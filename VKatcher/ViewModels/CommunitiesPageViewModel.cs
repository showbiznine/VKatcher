using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using VKatcher.Services;
using VKatcher.Views;
using Windows.UI.Xaml;
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
        public RelayCommand GoToSettingsCommand { get; set; }
        public RelayCommand GoToMyMusicCommand { get; set; }
        public RelayCommand<VKGroup> SelectGroup { get; set; }
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
            }
                );
            GoToSettingsCommand = new RelayCommand(() =>
            {
                _navigationService.NavigateTo(typeof(SettingsPage));
            });
            GoToMyMusicCommand = new RelayCommand(() =>
            {
                _navigationService.NavigateTo(typeof(MyMusicPage));
            });
            SelectGroup = new RelayCommand<VKGroup>(group =>
            {
                _navigationService.NavigateTo(typeof(FeedPage), group);
            });
        }

        private async void LoadGroups()
        {
            _inCall = true;
            _results.Clear();
            await DispatcherHelper.RunAsync(async () =>
            {
                _results = await Services.DataService.LoadMyGroups();

            });
            _inCall = false;
        }
    }
}
