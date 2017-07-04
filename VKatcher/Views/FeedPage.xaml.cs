using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using VKatcher.Services;
using VKatcher.ViewModels;
using VKatcherShared.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace VKatcher.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FeedPage : Page
    {
        private FeedPageViewModel VM;

        public FeedPage()
        {
            this.InitializeComponent();
            lstHeader.Loaded += LstHeader_Loaded;
        }

        private void LstHeader_Loaded(object sender, RoutedEventArgs e)
        {
            lstHeader.Mode = Microsoft.Toolkit.Uwp.UI.Controls.ScrollHeaderMode.QuickReturn;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private async Task CheckSubscribed(VKGroup group)
        {
            var groups = await SubscriptionService.LoadSubscribedGroups();
            foreach (var g in groups)
            {
                if (g.id == group.id)
                {
                    group.IsSubscribed = true;
                    return;
                }
            }
            group.IsSubscribed = false;
        }

        private void ellGroupImage_ImageExOpened(object sender, Microsoft.Toolkit.Uwp.UI.Controls.ImageExOpenedEventArgs e)
        {
            var anim = ConnectedAnimationService.GetForCurrentView().GetAnimation("groupImage");
            if (anim != null)
                anim.TryStart(ellGroupImage);
        }
    }
}
