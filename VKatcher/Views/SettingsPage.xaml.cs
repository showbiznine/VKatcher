using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using VKatcherShared.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace VKatcher.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
        }

        private void btnClearDLs_Click(object sender, RoutedEventArgs e)
        {
            FileService.ClearDownloads();
        }

        private async void btnLastID_Click(object sender, RoutedEventArgs e)
        {
            var groups = await SubscriptionService.LoadSubscribedGroups();
            foreach (var group in groups)
            {
                group.last_id = 0;
            }
            await SubscriptionService.WriteSubscribedGroups(groups);
        }
    }
}
