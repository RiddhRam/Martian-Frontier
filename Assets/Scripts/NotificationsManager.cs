using System;
using System.Threading.Tasks;
//using Unity.Services.PushNotifications;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public class NotificationsManager : MonoBehaviour {
    public async void Start() {
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

        // Wait for initialization in Cloud Delegator
        await Task.Delay(5000);
        /*
        try
        {
            string pushToken = await PushNotificationsService.Instance.RegisterForPushNotificationsAsync();

            PushNotificationsService.Instance.OnRemoteNotificationReceived += notificationData =>
            {
                Debug.Log("Received a notification!");
            };
        }
        catch (Exception e)
        {
            Debug.Log("Failed to retrieve a push notification token: " + e.Message);
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

