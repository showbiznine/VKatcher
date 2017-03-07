using Microsoft.QueryStringDotNET;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.System.RemoteSystems;
using Windows.UI.Popups;

namespace VKatcher.Services
{
    public class RemoteSystemService
    {
        private static RemoteSystemWatcher _remoteSystemWatcher;
        public static ObservableCollection<RemoteSystem> _deviceList = new ObservableCollection<RemoteSystem>();
        private static Dictionary<string, RemoteSystem> _deviceMap = new Dictionary<string, RemoteSystem>();

        #region Watcher
        public static async Task BuildDeviceListAsync()
        {
            RemoteSystemAccessStatus accessStatus = await RemoteSystem.RequestAccessAsync();

            if (accessStatus == RemoteSystemAccessStatus.Allowed)
            {
                _remoteSystemWatcher = RemoteSystem.CreateWatcher();

                // Subscribing to the event raised when a new remote system is found by the watcher.
                _remoteSystemWatcher.RemoteSystemAdded += RemoteSystemWatcher_RemoteSystemAdded;

                // Subscribing to the event raised when a previously found remote system is no longer available.
                _remoteSystemWatcher.RemoteSystemRemoved += RemoteSystemWatcher_RemoteSystemRemoved;

                _remoteSystemWatcher.Start();
            }
        }

        public static void GetNearbyDevices()
        {
            _remoteSystemWatcher.Stop();
            _deviceList.Clear();
            _deviceMap.Clear();
            // store filter list
            List<IRemoteSystemFilter> listOfFilters = makeFilterList();

            // construct watcher with the list
            _remoteSystemWatcher = RemoteSystem.CreateWatcher(listOfFilters);

            // Subscribing to the event raised when a new remote system is found by the watcher.
            _remoteSystemWatcher.RemoteSystemAdded += RemoteSystemWatcher_RemoteSystemAdded;

            // Subscribing to the event raised when a previously found remote system is no longer available.
            _remoteSystemWatcher.RemoteSystemRemoved += RemoteSystemWatcher_RemoteSystemRemoved;

            _remoteSystemWatcher.Start();
        }

        private static List<IRemoteSystemFilter> makeFilterList()
        {
            // construct an empty list
            List<IRemoteSystemFilter> localListOfFilters = new List<IRemoteSystemFilter>();

            // construct a discovery type filter that only allows "proximal" connections:
            RemoteSystemDiscoveryTypeFilter discoveryFilter = new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Proximal);


            // construct a device type filter that only allows desktop and mobile devices:
            // For this kind of filter, we must first create an IIterable of strings representing the device types to allow.
            // These strings are stored as static read-only properties of the RemoteSystemKinds class.
            List<String> listOfTypes = new List<String>();
            listOfTypes.Add(RemoteSystemKinds.Desktop);
            listOfTypes.Add(RemoteSystemKinds.Phone);
            listOfTypes.Add(RemoteSystemKinds.Xbox);

            // Put the list of device types into the constructor of the filter
            RemoteSystemKindFilter kindFilter = new RemoteSystemKindFilter(listOfTypes);


            // construct an availibility status filter that only allows devices marked as available:
            RemoteSystemStatusTypeFilter statusFilter = new RemoteSystemStatusTypeFilter(RemoteSystemStatusType.Available);


            // add the 3 filters to the listL
            localListOfFilters.Add(discoveryFilter);
            localListOfFilters.Add(kindFilter);
            localListOfFilters.Add(statusFilter);

            // return the list
            return localListOfFilters;
        }

        private static void RemoteSystemWatcher_RemoteSystemRemoved(RemoteSystemWatcher sender, RemoteSystemRemovedEventArgs args)
        {
            if (_deviceMap.ContainsKey(args.RemoteSystemId))
            {
                _deviceList.Remove(_deviceMap[args.RemoteSystemId]);
                _deviceMap.Remove(args.RemoteSystemId);
            }
        }

        private static void RemoteSystemWatcher_RemoteSystemAdded(RemoteSystemWatcher sender, RemoteSystemAddedEventArgs args)
        {
            _deviceList.Add(args.RemoteSystem);
            _deviceMap.Add(args.RemoteSystem.Id, args.RemoteSystem);
        }
        #endregion

        #region Casting
        public static async Task PlayAudioOnRemoteDeviceAsync(VKAudio audio, RemoteSystem system)
        {
            var q = new QueryString
            {
                {"action", "play_audio" },
                {"audio_id", audio.id.ToString() },
                {"owner_id", audio.owner_id.ToString() }
            };

            RemoteLaunchUriStatus launchUriStatus =
                    await RemoteLauncher.LaunchUriAsync(
                        new RemoteSystemConnectionRequest(system),
                        new Uri("vkatcher:?" + q));
            if (launchUriStatus != RemoteLaunchUriStatus.Success)
            {
                await new MessageDialog("Unable to connect to remote device").ShowAsync();
            }
        }

        public static async Task PlayAudioOnRemoteDeviceAsync(ObservableCollection<VKAudio> audios, RemoteSystem system, int index)
        {
            //var q = new QueryString
            //{
            //    {"action", "play_audios" },
            //    {"audio_id", audios[index].id.ToString() },
            //    {"owner_id", audios[index].owner_id.ToString() },
            //    {"url", audios[index].url }
            //};

            //RemoteLaunchUriStatus launchUriStatus =
            //        await RemoteLauncher.LaunchUriAsync(
            //            new RemoteSystemConnectionRequest(system),
            //            new Uri("vkatcher:?" + q));

            var appService = new AppServiceConnection()
            {
                AppServiceName = "com.vkatcher.playlist",
                PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName
            };

            RemoteSystemConnectionRequest connectionRequest = new RemoteSystemConnectionRequest(system);
            var status = await appService.OpenRemoteAsync(connectionRequest);

            if (status == AppServiceConnectionStatus.Success)
            {
                var message = new ValueSet();
                foreach (var item in audios)
                {
                    message.Add(item.id.ToString(), item.url);
                }
                var response = await appService.SendMessageAsync(message);
            }
        }

        public static async void OnRequestRevievedAsync(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var messageDeferral = args.GetDeferral();

            ValueSet message = args.Request.Message;
            ValueSet returnData = new ValueSet();

            if (message.ContainsKey("Command"))
            {
                string command = message["Command"] as string;
                // ... // 
            }
            else
            {
                returnData.Add("Status", "Fail: Missing command");
            }

            await args.Request.SendResponseAsync(returnData); // Return the data to the caller. 
            messageDeferral.Complete();
        }
        #endregion

    }
}
