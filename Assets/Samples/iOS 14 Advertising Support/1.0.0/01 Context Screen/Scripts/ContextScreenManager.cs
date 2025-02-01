using System.Collections;
using Unity.Advertisement.IosSupport.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Advertisement.IosSupport.Samples
{
    /// <summary>
    /// This component will trigger the context screen to appear when the scene starts,
    /// if the user hasn't already responded to the iOS tracking dialog.
    /// </summary>
    public class ContextScreenManager : MonoBehaviour
    {
        /// <summary>
        /// The prefab that will be instantiated by this component.
        /// The prefab has to have an ContextScreenView component on its root GameObject.
        /// </summary>
        public ContextScreenView contextScreenPrefab;

        void Start()
        {
            // APG = Ad Permission Given, 0 = false, 1 = true
            PlayerPrefs.SetInt("APG", 1);
            StartCoroutine(WaitForGDPRThenRequest());
        }

        private IEnumerator WaitForGDPRThenRequest() {
            if (!Debug.isDebugBuild) {
                #if UNITY_IOS
                // check with iOS to see if the user has accepted or declined tracking
                var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();

                if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
                    var contextScreen = Instantiate(contextScreenPrefab).GetComponent<ContextScreenView>();

                    // after the Continue button is pressed, and the tracking request
                    // has been sent, automatically destroy the popup to conserve memory
                    //contextScreen.sentTrackingAuthorizationRequest += () => Destroy(contextScreen.gameObject);
                    contextScreen.RequestAuthorizationTracking();
                }
                yield return new WaitUntil(() => ATTrackingStatusBinding.GetAuthorizationTrackingStatus() != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED);

                if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == ATTrackingStatusBinding.AuthorizationTrackingStatus.DENIED) {
                    PlayerPrefs.SetInt("APG", 0);
                }
                #endif
            }
        
            yield return null;
            SceneManager.LoadScene(1);
        }
    }   
}
