using System;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AdConsent : MonoBehaviour
{
    public DataPersistenceManager dataPersistenceManager;

    #if UNITY_IPHONE || UNITY_IOS

    void Awake()
    {
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
    }   

    public void UpdatePlayerStatus(bool doneTutorialStatus) {
        if (doneTutorialStatus) {
            GetAdConsent();
            return;
        }

        SceneManager.LoadScene("Loading Screen");
    }

    public void GetAdConsent() {
        Debug.Log("GETTING AD CONSENt");
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
                SceneManager.LoadScene("Loading Screen");
                return;
            }

            // If the error is null, the consent information state was updated.
            // You are now ready to check if a form is available.
            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
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
                    PlayerPrefs.SetString("APG", "Allowed");
                } else {
                    PlayerPrefs.SetString("APG", "Not Allowed");
                }

                if (PlayerPrefs.GetString("iOSATT") != "Responded") {
                    if (Application.isEditor) {
                        SceneManager.LoadScene("Loading Screen");
                        return;
                    }

                    SceneManager.LoadScene("iOS ATT");
                    return;
                }

                SceneManager.LoadScene("Loading Screen");
            });
        } catch (Exception ex) {
            Debug.LogError("Consent info error: " + ex.Message);
            SceneManager.LoadScene("Loading Screen");
        }
    }

    #elif UNITY_ANDROID
    void Awake() {
        SceneManager.LoadScene("Loading Screen");
    }

    #endif

}