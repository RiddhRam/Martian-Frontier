using UnityEngine;
using UnityEngine.SceneManagement;

public class AdConsent : MonoBehaviour
{
    public DataPersistenceManager dataPersistenceManager;

    #if UNITY_IPHONE || UNITY_IOS

    void Awake()
    {
        // Reset everything
        /*PlayerPrefs.SetString("APG", "");
        PlayerPrefs.SetString("iOSATT", "");
        ConsentInformation.Reset();*/

        if (PlayerPrefs.GetString("APG") == "Allowed" || PlayerPrefs.GetString("APG") == "Not Allowed") {
            
            if (PlayerPrefs.GetString("iOSATT") != "Responded") {
                if (Application.isEditor) {
                    SceneManager.LoadScene("Loading Screen");
                    return;
                }
                
                SceneManager.LoadScene("iOS ATT");
                return;
            }

            SceneManager.LoadScene("Loading Screen");
            return;
        }

        if (PlayerPrefs.GetString("iOSATT") != "Responded") {
            if (Application.isEditor) {
                SceneManager.LoadScene("Loading Screen");
                return;
            }

            SceneManager.LoadScene("iOS ATT");
        }

        if (PlayerPrefs.GetString("iOSATT") == "Responded") {
            SceneManager.LoadScene("Loading Screen");
            return;
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
            
            // Create a ConsentRequestParameters object.\
            ConsentRequestParameters request = new();

            // Check the current consent information status.
            ConsentInformation.Update(request, OnConsentInfoUpdated);
        } catch (Exception ex) {
            Debug.LogError("Get consent error:" + ex.Message);
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
        }
    }

    #elif UNITY_ANDROID
    void Awake() {
        SceneManager.LoadScene("Loading Screen");
    }

    #endif

}