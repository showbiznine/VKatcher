using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace VKatcher.Services
{
    public class AppDataService
    {
        public static ApplicationDataContainer LocalSettings { get; set; }
        public static ApplicationDataContainer RoamingSettings { get; set; }

        public static StorageFolder LocalFolder { get; private set; }
        public static StorageFolder RoamingFolder { get; private set; }

        public static void Setup()
        {
            LocalSettings = ApplicationData.Current.LocalSettings;
            LocalFolder = ApplicationData.Current.LocalFolder;
            RoamingSettings = ApplicationData.Current.RoamingSettings;
            RoamingFolder = ApplicationData.Current.RoamingFolder;
        }

        public static void ClearAllSettings()
        {
            ClearLocalSettings();
            ClearRoamingSettings();

        }

        #region Local settings
        public static object GetLocalSetting(string settingName)
        {
            return LocalSettings.Values[settingName];
        }

        public static void SetLocalSetting(string settingName, object value)
        {
            if (LocalSettings.Values[settingName] != null)
                LocalSettings.Values[settingName] = value;
            else
                LocalSettings.Values.Add(settingName, value);
        }

        public static void ClearLocalSettings()
        {
            LocalSettings.Values.Clear();
        }
        #endregion

        #region Roaming settings
        public static object GetRoamingSetting(string settingName)
        {
            return RoamingSettings.Values[settingName];
        }

        public static void SetRoamingSetting(string settingName, object value)
        {
            if (RoamingSettings.Values[settingName] != null)
                RoamingSettings.Values[settingName] = value;
            else
                RoamingSettings.Values.Add(settingName, value);
        }

        public static void ClearRoamingSettings()
        {
            LocalSettings.Values.Clear();
        }
        #endregion
    }
}
