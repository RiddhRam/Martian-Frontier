using System;
using System.Threading.Tasks;
using Unity.Services.PushNotifications;
using UnityEngine;

public class NotificationsManager : MonoBehaviour {
    public async void Start() {
        // Wait for initialization in Cloud Delegator
        await Task.Delay(5000);

        try
        {
            string pushToken = await PushNotificationsService.Instance.RegisterForPushNotificationsAsync();
        }
        catch (Exception e)
        {
            Debug.Log("Failed to retrieve a push notification token: " + e.Message);
        }
    }
}

