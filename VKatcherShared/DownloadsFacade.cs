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
using VKatcherShared.Models;
using Windows.Storage;

namespace VKatcherShared.Services
{
    public class DownloadsFacade
    {
        #region Fields
        private const string _downloadedDB = "downloaded_files.json";
        private static ObservableCollection<DownloadedTrack> _myDLs;
        private static StorageFile _file; 
        #endregion

        public DownloadsFacade()
        {
            //_myDLs.CollectionChanged += _myDLs_CollectionChanged;
        }

        private void _myDLs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Write to file
            string newstr = JsonConvert.SerializeObject(_myDLs);
            File.WriteAllText(_file.Path, newstr);
        }

        public static async void AddToDownloadsList(VKAttachment item, StorageFile file)
        {
            try
            {
                var temp = await ApplicationData.Current.RoamingFolder.GetFileAsync(_downloadedDB);
                _file = temp;
                var str = File.ReadAllText(temp.Path);
                _myDLs = new ObservableCollection<DownloadedTrack>();
                _myDLs = JsonConvert.DeserializeObject<ObservableCollection<DownloadedTrack>>(str);
                var d = new DownloadedTrack();
                d.artist = item.audio.artist;
                d.title = item.audio.title;
                d.file_path = file.Path;
                d.duration = item.audio.duration;

                var tempcol = _myDLs;
                tempcol.Add(d);
                string newstr = JsonConvert.SerializeObject(tempcol);
                File.WriteAllText(_file.Path, newstr);
            }
            catch (Exception ex)
            {
                //var file = await ApplicationData.Current.RoamingFolder.CreateFileAsync(_downloadedDB);
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
