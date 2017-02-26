using Microsoft.QueryStringDotNET;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using Windows.System;
using Windows.System.RemoteSystems;

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
        }
        #endregion

    }
}
