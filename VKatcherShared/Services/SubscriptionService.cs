using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using Windows.Storage;

namespace VKatcherShared.Services
{
    public class SubscriptionService
    {
        private const string _filePath = "followed_groups.json";

        public static async Task<ObservableCollection<VKGroup>> LoadSubscribedGroups()
        {
            var groups = new ObservableCollection<VKGroup>();
            StorageFile file;
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync(_filePath);
            }
            catch (Exception)
            {
                file = await ApplicationData.Current.LocalFolder.CreateFileAsync(_filePath);
            }
            string str = File.ReadAllText(file.Path);
            groups = JsonConvert.DeserializeObject<ObservableCollection<VKGroup>>(str);
            if (groups == null)
            {
                groups = new ObservableCollection<VKGroup>();
            }
            return groups;
        }

        public static async Task<bool> SubscribeToGroup(VKGroup group)
        {
            try
            {
                var subbedGroups = await LoadSubscribedGroups();
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(_filePath);
                if (!group.IsSubscribed)
                {
                    group.IsSubscribed = true;
                    subbedGroups.Add(group);
                }
                else
                {
                    var currentGroup = from gr in subbedGroups
                                       where gr.id == @group.id
                                       select gr;
                    subbedGroups.Remove(currentGroup.ElementAt(0));
                }
                string newstr = JsonConvert.SerializeObject(subbedGroups);
                File.WriteAllText(file.Path, newstr);
                return !group.IsSubscribed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return group.IsSubscribed;
            }
        }

        public static async Task WriteSubscribedGroups(ObservableCollection<VKGroup> subscribedGroups)
        {
            var subscribedFile = await ApplicationData.Current.LocalFolder.GetFileAsync(_filePath);
            string newstr = JsonConvert.SerializeObject(subscribedGroups);
            File.WriteAllText(subscribedFile.Path, newstr);
            Debug.WriteLine("Wrote to groups database");
        }

    }
}
