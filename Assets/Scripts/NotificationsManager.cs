#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

using UnityEngine;
using System;

public class NotificationsManager : MonoBehaviour
{
    private const string ScheduledTimeKey = "TournamentNotificationTime";
    private const string tournamentResetTitle = "Tournament Reset!";
    private const string tournamentResetText = "Come collect your rewards and join the next round!";
    private const string channelId = "tournament_channel";

    void Start()
    {        
        TryScheduleTournamentNotification();
    }

    void TryScheduleTournamentNotification()
    {
        #if UNITY_ANDROID
        // Create the notification channel once at startup
        var channel = new AndroidNotificationChannel()
        {
            Id = channelId,
            Name = "Tournament Alerts",
            Importance = Importance.Default,
            Description = "Get notified when tournaments reset.",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        
        // Request notification permission explicitly for Android
        if (ShouldRequestNotificationPermission())
        {
            RequestNotificationPermission();
        }
        #endif

        DateTime nextTournamentTime = GetNextTournamentTime();

        if (PlayerPrefs.HasKey(ScheduledTimeKey))
        {
            DateTime previouslyScheduled = DateTime.Parse(PlayerPrefs.GetString(ScheduledTimeKey));

            // If it's already scheduled and hasn't changed, skip
            if (Mathf.Abs((float)(nextTournamentTime - previouslyScheduled).TotalMinutes) < 1f)
            {
                Debug.Log("Notification already scheduled for: " + previouslyScheduled);
                return;
            }
        }

        ScheduleTournamentNotification(nextTournamentTime);
        PlayerPrefs.SetString(ScheduledTimeKey, nextTournamentTime.ToString());
    }

    public void ScheduleTournamentNotification(DateTime fireTime)
    {
        #if UNITY_ANDROID
        var channel = new AndroidNotificationChannel()
        {
            Id = channelId,
            Name = "Tournament Alerts",
            Importance = Importance.Default,
            Description = "Get notified when tournaments reset.",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        var notification = new AndroidNotification
        {
            Title = tournamentResetTitle,
            Text = tournamentResetText,
            SmallIcon = "notification_small",
            LargeIcon = "notification_large",
            FireTime = fireTime,
            ShouldAutoCancel = true,
            ShowTimestamp = true,
        };

        AndroidNotificationCenter.SendNotification(notification, channelId);
        #endif

        #if UNITY_IOS
        var calendarTrigger = new iOSNotificationCalendarTrigger
        {
            Year = fireTime.Year,
            Month = fireTime.Month,
            Day = fireTime.Day,
            Hour = fireTime.Hour,
            Minute = fireTime.Minute,
            Second = fireTime.Second,
            Repeats = false
        };

        /*var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = TimeSpan.FromSeconds(30),
            Repeats = false
        };*/

        var notification = new iOSNotification()
        {
            Identifier = "tournament_reward",
            Title = tournamentResetTitle,
            Body = tournamentResetText,
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = "reward_category",
            ThreadIdentifier = "reward_thread",
            Trigger = calendarTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(notification);
        #endif
    }

    private DateTime GetNextTournamentTime()
    {
        // Tournament resets at 12 PM UTC every 2 days since epoch
        DateTime now = DateTime.UtcNow;
        DateTime lastReset = new DateTime(1970, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        while (lastReset <= now)
        {
            lastReset = lastReset.AddDays(2);
        }
        return lastReset.ToLocalTime(); // Convert to local time for display/notification
    }

    #if UNITY_ANDROID
    public static bool ShouldRequestNotificationPermission()
    {
        if (Application.platform != RuntimePlatform.Android) return false;

        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            int sdkInt = version.GetStatic<int>("SDK_INT");
            return sdkInt >= 33; // Android 13 (Tiramisu)
        }
    }

    public static void RequestNotificationPermission()
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            using (var permissionRequester = new AndroidJavaObject("androidx.core.app.ActivityCompat"))
            {
                permissionRequester.CallStatic("requestPermissions", activity, new string[] { "android.permission.POST_NOTIFICATIONS" }, 0);
            }
        }
    }
    #endif
}
