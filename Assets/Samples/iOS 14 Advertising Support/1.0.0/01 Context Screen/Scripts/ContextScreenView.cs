using System;
using UnityEngine;

namespace Unity.Advertisement.IosSupport.Components
{
    /// <summary>
    /// This component controls an iOS App Tracking Transparency context screen.
    /// You should only have one of these in your app.
    /// </summary>
    public sealed class ContextScreenView : MonoBehaviour
    {
        /// <summary>
        /// This event will be invoked after the ContinueButton is clicked
        /// and after the tracking authorization request has been sent.
        /// It's a good idea to subscribe to this event so you can destroy
        /// this GameObject to free up memory after it's no longer needed.
        /// Once the tracking authorization request has been sent, there's no
        /// need for this popup again until the app is uninstalled and reinstalled.
        /// </summary>
        public event Action sentTrackingAuthorizationRequest;

        public void RequestAuthorizationTracking()
        {
#if UNITY_IPHONE || UNITY_IOS
            ATTrackingStatusBinding.RequestAuthorizationTracking();

            sentTrackingAuthorizationRequest?.Invoke();
#endif
        }
    }
}
