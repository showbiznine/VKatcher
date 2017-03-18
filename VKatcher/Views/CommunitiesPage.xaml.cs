using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using VK.WindowsPhone.SDK.API.Model;
using VKatcher.ViewModels;
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
    public sealed partial class CommunitiesPage : Page
    {
        public CommunitiesPage()
        {
            this.InitializeComponent();
        }

        private void gridView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var panel = (ItemsWrapGrid)gridView.ItemsPanelRoot;
            int i = 1;

            if (gridView.ActualWidth < 500)
                i = 1;
            else if (gridView.ActualWidth < 700)
                i = 2;
            else if (gridView.ActualWidth < 1000)
                i = 3;
            else if (gridView.ActualWidth < 1500)
                i = 4;

            panel.MaximumRowsOrColumns = i;
            panel.ItemWidth = panel.ItemHeight = (e.NewSize.Width - 12) / i;
        }

        private void gridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as VKGroup;

            var root = (e.OriginalSource as GridView).ContainerFromItem(e.ClickedItem) as GridViewItem;
            var uc = root.ContentTemplateRoot as UserControl;
            var image = ((uc.Content as DropShadowPanel).Content as Grid).FindDescendantByName("image");

            var cas = ConnectedAnimationService.GetForCurrentView();
            cas.PrepareToAnimate("groupImage", image);
        }
    }
}
