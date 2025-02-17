using UnityEngine;
using GoogleMobileAds.Ump.Api;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public class AdConsent : MonoBehaviour
{
    public DataPersistenceManager dataPersistenceManager;
    void Awake()
    {
        // Reset everything
        PlayerPrefs.SetString("APG", "");
        ConsentInformation.Reset();

        if (PlayerPrefs.GetString("APG") == "Allowed" || PlayerPrefs.GetString("APG") == "Not Allowed") {
            Debug.Log(PlayerPrefs.GetString("APG"));
            SceneManager.LoadScene("Singleplayer");
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer || Application.isEditor) {
            if (!(PlayerPrefs.GetString("iOSATT") == "Responded")) {
                SceneManager.LoadScene("Singleplayer");
            }
        }

        Debug.Log("No response");
    }   

    public void UpdatePlayerStatus(bool newPlayerStatus) {

        if (newPlayerStatus) {
            Debug.Log("Ask!");
            GetAdConsent();
            return;
        }

        Debug.Log("Don't ask!");
        SceneManager.LoadScene("Singleplayer");
    }

    public void GetAdConsent() {
        try {
            // Only uncomment when debugging user consent settings
            var debugSettings = new ConsentDebugSettings
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
            };
            
            // Create a ConsentRequestParameters object.
            //ConsentRequestParameters request = new();

            // Check the current consent information status.
            ConsentInformation.Update(request, OnConsentInfoUpdated);
        } catch {
        }
    }

    void OnConsentInfoUpdated(FormError consentError)
    {

        try {
            if (consentError != null)
            {
                // Handle the error.
                Debug.LogError(consentError);
                return;
            }

            // If the error is null, the consent information state was updated.
            // You are now ready to check if a form is available.
            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                if (formError != null)
                {
                    // Consent gathering failed.
                    Debug.LogError(consentError);
                    return;
                }

                // 0 = no consent
                // 1 = consent
                // Consent has been gathered.
                if (ConsentInformation.CanRequestAds())
                {
                    PlayerPrefs.SetString("APG", "Allowed");
                    Debug.Log("Allowed");
                    //FillEmptyAdSlots();
                } else {
                    PlayerPrefs.SetString("APG", "Not Allowed");
                    Debug.Log("Not Allowed");
                }

                if (Application.platform == RuntimePlatform.IPhonePlayer || Application.isEditor) {
                    if (!(PlayerPrefs.GetString("iOSATT") != "Responded")) {
                        SceneManager.LoadScene("iOS ATT");
                        return;
                    }
                }

                SceneManager.LoadScene("Singleplayer");
            });
        } catch {
        }
    }

}
