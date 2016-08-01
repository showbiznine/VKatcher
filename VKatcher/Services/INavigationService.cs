using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;

namespace VKatcher.Services
{
    public interface INavigationService
    {
        void NavigateTo(Type Page);
        void NavigateTo(Type Page, object parameter);
        void NavigateTo(Type Page, object parameter, NavigationTransitionInfo info);
        void GoBack();
    }
}
