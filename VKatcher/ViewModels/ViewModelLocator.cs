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
            SimpleIoc.Default.Register<FeedPageViewModel>();
            SimpleIoc.Default.Register<SettingsPageViewModel>();
            SimpleIoc.Default.Register<SearchPageViewModel>();
            SimpleIoc.Default.Register<MyMusicPageViewModel>();
            SimpleIoc.Default.Register<NowPlayingPageViewModel>();
        }

        #region ViewModels
        public MainViewModel Main
        {
            get { return ServiceLocator.Current.GetInstance<MainViewModel>();}
        }
        public FeedPageViewModel Feed
        {
            get { return ServiceLocator.Current.GetInstance<FeedPageViewModel>(); }
        }
        public CommunitiesPageViewModel Communities
        {
            get { return ServiceLocator.Current.GetInstance<CommunitiesPageViewModel>(); }
        }
        public SettingsPageViewModel Settings
        {
            get { return ServiceLocator.Current.GetInstance<SettingsPageViewModel>(); }
        }
        public SearchPageViewModel Search
        {
            get { return ServiceLocator.Current.GetInstance<SearchPageViewModel>(); }
        }
        public MyMusicPageViewModel MyMusic
        {
            get { return ServiceLocator.Current.GetInstance<MyMusicPageViewModel>(); }
        }
        public NowPlayingPageViewModel NowPlaying
        {
            get { return ServiceLocator.Current.GetInstance<NowPlayingPageViewModel>(); }
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
