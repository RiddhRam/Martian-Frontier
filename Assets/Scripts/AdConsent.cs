using System;
using System.Collections.Generic;
using GoogleMobileAds.Mediation.UnityAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AdConsent : MonoBehaviour
{
    private static readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    void Awake()
    {
        #if UNITY_IPHONE || UNITY_IOS

        try {
            // Reset everything
            /*PlayerPrefs.SetString("APG", "");
            PlayerPrefs.SetString("iOSATT", "");
            ConsentInformation.Reset();*/

            // If not responded to iOS ATT, then go to iOS ATT
            if (PlayerPrefs.GetString("iOSATT") != "Responded") {
                if (Application.isEditor) {
                    SceneManager.LoadScene("Loading Screen");
                    return;
                }

                SceneManager.LoadScene("iOS ATT");
            }

        } catch {
            SceneManager.LoadScene("Loading Screen");
        }

        #elif UNITY_ANDROID

        // Don't show ads the first time, just go to the game
        try {

            if (PlayerPrefs.GetString("First Load") != "Loaded") {
                PlayerPrefs.SetString("First Load", "Loaded");
                SceneManager.LoadScene("Loading Screen");
            }

        } catch (Exception ex) {
            Debug.LogError(ex.Message);
            SceneManager.LoadScene("Loading Screen");
        }

        #endif
    }   
    
    void Update()
    {
        // Execute any queued actions on the main thread
        while (_mainThreadActions.Count > 0)
        {
            _mainThreadActions.Dequeue().Invoke();
        }
    }

    // A utility method to queue an action for the main thread
    public static void RunOnMainThread(Action action)
    {
        lock (_mainThreadActions)
        {
            _mainThreadActions.Enqueue(action);
        }
    }

    public void UpdatePlayerStatus(bool doneTutorialStatus)
    {
        if (doneTutorialStatus)
        {
            GetAdConsent();
            return;
        }

        SceneManager.LoadScene("Loading Screen");
    }

    public void GetAdConsent() {
        
        Debug.Log("GETTING AD CONSENT");
        try {
            // Only uncomment when debugging user consent settings
            /*var debugSettings = new ConsentDebugSettings
            {
                DebugGeography = DebugGeography.Other,
                TestDeviceHashedIds =
                new List<string>
                {
                    "93001fda-7fff-44e5-80b1-b086356f0b51"
                }
            };

            // Create a ConsentRequestParameters object.
            ConsentRequestParameters request = new ConsentRequestParameters
            {
                ConsentDebugSettings = debugSettings,
            };*/
            
            // Create a ConsentRequestParameters object.
            ConsentRequestParameters request = new();
            // Check the current consent information status.
            ConsentInformation.Update(request, OnConsentInfoUpdated);
        } catch (Exception ex) {
            Debug.LogError("Get consent error:" + ex.Message);
            SceneManager.LoadScene("Loading Screen");
        }
    }

    void OnConsentInfoUpdated(FormError consentError)
    {
        try {
            if (consentError != null)
            {
                // Handle the error.
                Debug.LogError(consentError);
                RunOnMainThread(() => SceneManager.LoadScene("Loading Screen"));
                return;
            }

            // If the error is null, the consent information state was updated.
            // You are now ready to check if a form is available.
            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                RunOnMainThread(() =>
                {
                    if (formError != null)
                    {
                        // Consent gathering failed.
                        Debug.LogError(formError);
                        SceneManager.LoadScene("Loading Screen");
                        return;
                    }

                    // Consent has been gathered.
                    if (ConsentInformation.CanRequestAds())
                    {
                        Debug.Log("SET UNITY CONSENT: TRUE");
                        UnityAds.SetConsentMetaData("gdpr.consent", true);
                        UnityAds.SetConsentMetaData("privacy.consent", true);

                        PlayerPrefs.SetString("APG", "Allowed");
                    }
                    else
                    {
                        Debug.Log("SET UNITY CONSENT: FALSE");
                        UnityAds.SetConsentMetaData("gdpr.consent", false);
                        UnityAds.SetConsentMetaData("privacy.consent", false);

                        PlayerPrefs.SetString("APG", "Not Allowed");
                    }

                    #if UNITY_IPHONE || UNITY_IOS

                    if (PlayerPrefs.GetString("iOSATT") != "Responded") {
                        if (Application.isEditor) {
                            SceneManager.LoadScene("Loading Screen");
                            return;
                        }

                        SceneManager.LoadScene("iOS ATT");
                        return;
                    }

                    #endif

                    SceneManager.LoadScene("Loading Screen");
                });
            });
        } catch (Exception ex) {
            Debug.LogError("Consent info error: " + ex.Message);
            RunOnMainThread(() => SceneManager.LoadScene("Loading Screen"));
        }
    }

}