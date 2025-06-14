using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using Unity.Services.PushNotifications;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public class NotificationsManager : MonoBehaviour {
    public async void Start() {
        if (Debug.isDebugBuild) {
            return;
        }
        
        #if UNITY_ANDROID
        string requestNotifs = "";

        try {
            requestNotifs = PlayerPrefs.GetString("AskNotif");
        } catch {
        }

        // Request notification permission explicitly for Android
        if (ShouldRequestNotificationPermission() && requestNotifs != "No")
        {
            RequestNotificationPermission();
        }
        #endif

        await UnityServices.InitializeAsync();
        
        try
        {
            PushNotificationsService.Instance.OnRemoteNotificationReceived += PushNotificationRecieved;

            AnalyticsService.Instance.StartDataCollection();

            // Make sure to set the required settings in Project Settings before testing
            string token = await PushNotificationsService.Instance.RegisterForPushNotificationsAsync();
            //Debug.Log($"The push notification token is {token}");
            
        }
        catch (Exception e)
        {
            Debug.Log("Failed to retrieve a push notification token: " + e.Message);
        }
    }

    void PushNotificationRecieved(Dictionary<string, object> notificationData)
    {
        /*Debug.Log("Notification received!");
        foreach (KeyValuePair<string, object> item in notificationData)
        {
            Debug.Log($"Notification data item: {item.Key} - {item.Value}");
        }*/
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
        PlayerPrefs.SetString("AskNotif", "No");
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

