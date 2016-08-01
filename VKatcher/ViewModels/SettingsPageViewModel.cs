using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
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
using VKatcherShared.Services;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace VKatcher.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        public ObservableCollection<VKGroup> SubscribedGroups { get; set; }
        public INavigationService _navigationService { get { return ServiceLocator.Current.GetInstance<INavigationService>(); } }
        public RelayCommand<ItemClickEventArgs> SelectGroup { get; set; }
        public RelayCommand<VKGroup> UnsubscribeCommand { get; set; }

        public SettingsPageViewModel()
        {
            if (IsInDesignMode)
            {

            }
            else
            {
                InitializeGroups();
                InitializeCommands();
            }
        }

        private void InitializeCommands()
        {
            SelectGroup = new RelayCommand<ItemClickEventArgs>(args =>
            {
                var group = args.ClickedItem as VKGroup;
                _navigationService.NavigateTo(typeof(FeedPage), group);
            });
            UnsubscribeCommand = new RelayCommand<VKGroup>(async group =>
            {
                MessageDialog msg = new MessageDialog("Are you sure you want to unsubscribe from " +
                    group.name + "?");
                msg.Commands.Add(new UICommand("Yes", async (command) =>
                {
                    var res = await SubscriptionService.SubscribeToGroup(group);
                    if (!res)
                    {
                        SubscribedGroups.Remove(group);
                    }
                }));
                msg.Commands.Add(new UICommand("No"));
                msg.DefaultCommandIndex = 1;
                await msg.ShowAsync();
            });
        }

        private async void InitializeGroups()
        {
            SubscribedGroups = new ObservableCollection<VKGroup>();
            SubscribedGroups = await SubscriptionService.LoadSubscribedGroups();
        }
    }
}
