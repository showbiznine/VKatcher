using GalaSoft.MvvmLight.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VKatcher.Views;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace VKatcher.Services
{
    public class NavigationService : INavigationService
    {
        private Frame _frame { get; set; }

        public Page CurrentPage
        {
            get
            {
                _frame = ((Window.Current.Content as Frame).Content as MainPage).FindName("myFrame") as Frame;
                return _frame.Content as Page;
            }
        }

        public string CurrentPageKey
        {
            get
            {
                _frame = ((Window.Current.Content as Frame).Content as MainPage).FindName("myFrame") as Frame;
                return (_frame.Content as Page).Name;
            }
        }

        public void GoBack()
        {
            _frame = ((Window.Current.Content as Frame).Content as MainPage).FindName("myFrame") as Frame;
            if (_frame.CanGoBack)
            {
                _frame.GoBack();
            }
        }

        public void NavigateTo(Type page)
        {
            var temp = Window.Current.Content as Frame;
            var content = temp.Content as MainPage;
            _frame = content.FindName("myFrame") as Frame;
            _frame.Navigate(page);
        }

        public void NavigateTo(Type page, object parameter)
        {
            _frame = ((Window.Current.Content as Frame).Content as MainPage).FindName("myFrame") as Frame;
            _frame.Navigate(page, parameter);
        }

        public void NavigateTo(Type page, object parameter, NavigationTransitionInfo info)
        {
            _frame = ((Window.Current.Content as Frame).Content as MainPage).FindName("myFrame") as Frame;
            _frame.Navigate(page, parameter, info);
        }
    }
}
