using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using VKatcher.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.RemoteSystems;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace VKatcher.ContentDialogs
{
    public sealed partial class RemoteDeviceDialog : ContentDialog
    {
        public ObservableCollection<RemoteSystem> AvailableRemoteDevices { get; set; }
        public RemoteSystem SelectedRemoteDevice { get; set; }

        public RemoteDeviceDialog()
        {
            this.InitializeComponent();
            AvailableRemoteDevices = RemoteSystemService._deviceList;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Hide();
        }

        private void lstDevices_ItemClick(object sender, ItemClickEventArgs e)
        {
            SelectedRemoteDevice = e.ClickedItem as RemoteSystem;
            this.Hide();
        }
    }
}
