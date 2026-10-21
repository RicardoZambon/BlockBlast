#nullable enable
#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections;
using Unity.Notifications.Android;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The Daily reminder on Android, through Unity Mobile Notifications: one channel, and at most one reminder
    /// scheduled at a time. Android 13 and later ask the player for permission; older versions allow it.
    /// </summary>
    public sealed class AndroidReminders : IReminders
    {
        const string ChannelId = "daily";

        public IEnumerator RequestPermission(Action<bool> done)
        {
            var request = new PermissionRequest();
            while (request.Status == PermissionStatus.RequestPending) yield return null;
            done(request.Status == PermissionStatus.Allowed);
        }

        public void Schedule(DateTime fireUtc, string title, string body)
        {
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel(ChannelId, title, body, Importance.Default));
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.SendNotification(new AndroidNotification(title, body, fireUtc.ToLocalTime()), ChannelId);
        }
    }
}
#endif
