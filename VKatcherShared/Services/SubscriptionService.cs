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
        private const string _groupsFilePath = "followed_groups.json";
        private const string _tagsFilePath = "followed_tags.json";

        #region Groups
        public static async Task<ObservableCollection<VKGroup>> LoadSubscribedGroups()
        {
            var groups = new ObservableCollection<VKGroup>();
            StorageFile file;
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync(_groupsFilePath);
            }
            catch (Exception)
            {
                file = await ApplicationData.Current.LocalFolder.CreateFileAsync(_groupsFilePath);
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
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(_groupsFilePath);
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
            var subscribedFile = await ApplicationData.Current.LocalFolder.GetFileAsync(_groupsFilePath);
            string newstr = JsonConvert.SerializeObject(subscribedGroups);
            File.WriteAllText(subscribedFile.Path, newstr);
            Debug.WriteLine("Wrote to groups database");
        }
        #endregion

        #region Tags
        public static async Task<ObservableCollection<VKTag>> LoadSubscribedTags()
        {
            var tags = new ObservableCollection<VKTag>();
            StorageFile file;
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync(_tagsFilePath);
            }
            catch (Exception)
            {
                file = await ApplicationData.Current.LocalFolder.CreateFileAsync(_tagsFilePath);
            }
            string str = File.ReadAllText(file.Path);
            tags = JsonConvert.DeserializeObject<ObservableCollection<VKTag>>(str);
            if (tags == null)
            {
                tags = new ObservableCollection<VKTag>();
            }
            return tags;
        }

        public static async Task<bool> SubscribeToTag(VKTag tag)
        {
            try
            {
                var subbedTags = await LoadSubscribedTags();
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(_tagsFilePath);
                if (!tag.IsSubscribed)
                {
                    tag.IsSubscribed = true;
                    subbedTags.Add(tag);
                }
                else
                {
                    var currentTag = from gr in subbedTags
                                       where gr.tag == tag.tag
                                       select gr;
                    subbedTags.Remove(currentTag.ElementAt(0));
                }
                string newstr = JsonConvert.SerializeObject(subbedTags);
                File.WriteAllText(file.Path, newstr);
                return !tag.IsSubscribed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return tag.IsSubscribed;
            }
        }

        public static async Task WriteSubscribedTags(ObservableCollection<VKTag> subscribedTags)
        {
            var subscribedFile = await ApplicationData.Current.LocalFolder.GetFileAsync(_tagsFilePath);
            string newstr = JsonConvert.SerializeObject(subscribedTags);
            File.WriteAllText(subscribedFile.Path, newstr);
            Debug.WriteLine("Wrote to tags database");
        }
        #endregion
    }
}
