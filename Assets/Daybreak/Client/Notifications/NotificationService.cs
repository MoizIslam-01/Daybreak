using System;
using UnityEngine;
#if DAYBREAK_MOBILE_NOTIFICATIONS && UNITY_ANDROID
using Unity.Notifications.Android;
#endif

namespace Daybreak.Client
{
    /// <summary>
    /// Schedules an on-device daily reminder just after the 20:00 UTC resolve. No Firebase, no
    /// server push — because the resolve time is fixed, a repeating local notification is all we
    /// need. Guarded by DAYBREAK_MOBILE_NOTIFICATIONS so the project builds without the package;
    /// see MILESTONE-5.md for how to enable it.
    /// </summary>
    public static class NotificationService
    {
        private const string ChannelId = "daybreak_daily";
        private const int ReminderHourUtc = 20;
        private const int ReminderMinuteUtc = 5; // a few minutes after resolve

        public static void ScheduleDailyResultReminder()
        {
#if DAYBREAK_MOBILE_NOTIFICATIONS && UNITY_ANDROID
            // Android 13+ requires runtime permission; this prompts once (no-op on older versions).
            AndroidNotificationCenter.RequestNotificationPermission();

            var channel = new AndroidNotificationChannel
            {
                Id = ChannelId,
                Name = "Daybreak results",
                Importance = Importance.Default,
                Description = "Daily battle results"
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);

            // Reschedule cleanly so we never stack duplicates.
            AndroidNotificationCenter.CancelAllScheduledNotifications();

            var notification = new AndroidNotification
            {
                Title = "Daybreak",
                Text = "Your battles resolved — see how you did!",
                FireTime = NextUtc(ReminderHourUtc, ReminderMinuteUtc).ToLocalTime(),
                RepeatInterval = TimeSpan.FromDays(1)
            };
            AndroidNotificationCenter.SendNotification(notification, ChannelId);
            Debug.Log("[Daybreak] Daily reminder scheduled.");
#else
            Debug.Log("[Daybreak] Local notifications not enabled in this build (needs the Mobile " +
                      "Notifications package on an Android target).");
#endif
        }

        private static DateTime NextUtc(int hour, int minute)
        {
            var now = DateTime.UtcNow;
            var fire = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, DateTimeKind.Utc);
            if (fire <= now) fire = fire.AddDays(1);
            return fire;
        }
    }
}
