using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using Unity.Services.PushNotifications;
using UnityEngine;

public class PushNotificationExample : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {
        await UnityServices.InitializeAsync();

        PushNotificationsService.Instance.OnRemoteNotificationReceived += PushNotificationRecieved;

        // Note: This is the minimum required to ensure the events with the push notification data are sent correctly through Analytics.
        // In a real game you would need to handle privacy consent states here, see the Analytics documentation for more details:
        // https://docs.unity.com/ugs/en-us/manual/analytics/manual/manage-data-privacy
        AnalyticsService.Instance.StartDataCollection();

        // Make sure to set the required settings in Project Settings before testing
        string token = await PushNotificationsService.Instance.RegisterForPushNotificationsAsync();
        Debug.Log($"The push notification token is {token}");
    }

    void PushNotificationRecieved(Dictionary<string, object> notificationData)
    {
        Debug.Log("Notification received!");
        foreach (KeyValuePair<string, object> item in notificationData)
        {
            Debug.Log($"Notification data item: {item.Key} - {item.Value}");
        }
    }
}
