using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class SearchPage : Page
    {
        private bool _isSearchOpen = false;
        private bool _isResultsOpen = false;


        public SearchPage()
        {
            this.InitializeComponent();
            //autoSuggestBox.Loaded += (s, e) =>
            //{
            //    SearchOpen.Begin();
            //    _isSearchOpen = true;
            //    autoSuggestBox.Focus(FocusState.Programmatic);
            //};
            grdGroups.SizeChanged += grdGroups_SizeChanged;
        }

        private void grdGroups_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var panel = (ItemsWrapGrid)grdGroups.ItemsPanelRoot;

            if (grdGroups.ActualWidth < 500)
            {
                panel.MaximumRowsOrColumns = 2;
                panel.ItemWidth = panel.ItemHeight = e.NewSize.Width / 2;
            }
            else if (grdGroups.ActualWidth < 700)
            {
                panel.MaximumRowsOrColumns = 3;
                panel.ItemWidth = panel.ItemHeight = e.NewSize.Width / 3;
            }
            else if (grdGroups.ActualWidth < 1000)
            {
                panel.MaximumRowsOrColumns = 4;
                panel.ItemWidth = panel.ItemHeight = e.NewSize.Width / 4;
            }
            else if (grdGroups.ActualWidth < 1500)
            {
                panel.MaximumRowsOrColumns = 5;
                panel.ItemWidth = panel.ItemHeight = e.NewSize.Width / 5;
            }
        }


        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            //if (!_isSearchOpen)
            //{
            //    SearchOpen.Begin();
            //    _isSearchOpen = true;
            //}
            //else if (!_isResultsOpen)
            //{
            //    OpenResults.Begin();
            //    _isResultsOpen = true;
            //}
        }

        private void autoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            //if (!_isResultsOpen)
            //{
            //    OpenResults.Begin();
            //    _isResultsOpen = true;
            //}
        }
    }
}
