using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VKatcher.Services;
using VKatcher.Views;

namespace VKatcher.ViewModels
{
    public class ViewModelLocator
    {/// <summary>
     /// Initializes a new instance of the ViewModelLocator class.
     /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            if (ViewModelBase.IsInDesignModeStatic)
            {
                // Create design time view services and models
                SimpleIoc.Default.Register<INavigationService, NavigationService>();
            }
            else
            {
                // Create run time view services and models
                var navigationService = CreateNavigationSevice();
                SimpleIoc.Default.Register(() => navigationService);
            }

            SimpleIoc.Default.Register<MainViewModel>(true);
            SimpleIoc.Default.Register<CommunitiesPageViewModel>();
            SimpleIoc.Default.Register<FeedViewModel>();
            SimpleIoc.Default.Register<SettingsViewModel>();
            SimpleIoc.Default.Register<SearchViewModel>();
            SimpleIoc.Default.Register<MyMusicViewModel>();
            SimpleIoc.Default.Register<NowPlayingViewModel>();
        }

        #region ViewModels
        public MainViewModel Main
        {
            get { return ServiceLocator.Current.GetInstance<MainViewModel>();}
        }
        public FeedViewModel Feed
        {
            get { return ServiceLocator.Current.GetInstance<FeedViewModel>(); }
        }
        public CommunitiesPageViewModel Communities
        {
            get { return ServiceLocator.Current.GetInstance<CommunitiesPageViewModel>(); }
        }
        public SettingsViewModel Settings
        {
            get { return ServiceLocator.Current.GetInstance<SettingsViewModel>(); }
        }
        public SearchViewModel Search
        {
            get { return ServiceLocator.Current.GetInstance<SearchViewModel>(); }
        }
        public MyMusicViewModel MyMusic
        {
            get { return ServiceLocator.Current.GetInstance<MyMusicViewModel>(); }
        }
        public NowPlayingViewModel NowPlaying
        {
            get { return ServiceLocator.Current.GetInstance<NowPlayingViewModel>(); }
        }
        #endregion

        public INavigationService CreateNavigationSevice()
        {
            var navigationService = new NavigationService();
            return navigationService;
        }

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }

}
