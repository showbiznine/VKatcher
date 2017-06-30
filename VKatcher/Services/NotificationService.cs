using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VK.WindowsPhone.SDK.API.Model;
using Windows.UI.Notifications;

namespace VKatcher.Services
{
    public class NotificationService
    {
        #region Toasts

        public static void PopToastGeneric(int count)
        {
            string body;
            if (count > 1)
                body = "We have " + count + " new tracks for you to check out!";
            else
                body = "We have a new track for you to check out!";

            #region Toast Visual
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText() {Text = body},
                    },
                }
            };
            #endregion

            ToastContent toastContent = new ToastContent()
            {
                Visual = visual,
                ActivationType = ToastActivationType.Foreground,
                Launch = "page=downloads"
            };

            var toast = new ToastNotification(toastContent.GetXml());
            toast.NotificationMirroring = NotificationMirroring.Allowed;
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        public static ToastNotification PopToastTrack(VKAudio track)
        {
            string body = "We have a new track for you!";
            string sub = string.Format("Check out {0} by {1}", track.title, track.artist);

            #region Toast Visual
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText() {Text = body},
                        new AdaptiveText() {Text = sub}
                    },
                }
            };
            #endregion

            ToastContent toastContent = new ToastContent()
            {
                Visual = visual,
                ActivationType = ToastActivationType.Foreground,
                Launch = "page=downloads&id=" + track.id
            };

            var toast = new ToastNotification(toastContent.GetXml());
            toast.NotificationMirroring = NotificationMirroring.Allowed;
            return toast;
            //ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        public static void PopToastCommunity(VKGroup group, int count)
        {
            string body = string.Format("{0} has {1} new tracks", group.name, count);

            #region Toast Visual
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText() {Text = body}
                    },
                    AppLogoOverride = new ToastGenericAppLogo
                    {
                        Source = group.photo_50,
                        HintCrop = ToastGenericAppLogoCrop.Circle
                    }
                }
            };
            #endregion

            ToastContent toastContent = new ToastContent()
            {
                Visual = visual,
                ActivationType = ToastActivationType.Foreground,
                Launch = "page=group&id=" + group.id
            };

            var toast = new ToastNotification(toastContent.GetXml());
            toast.NotificationMirroring = NotificationMirroring.Allowed;
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
        #endregion

        #region Tiles

        #endregion
    }
}
