using TMPro;
using UnityEngine;
#if UNITY_ANDROID
using Google.Play.Review;
#endif
using System.Collections;

public class AskForReview : MonoBehaviour
{
    public int responseTracker = 0;
    public GameObject safeArea;
    public GameObject feedbackScreen;
    private Transform safeAreaTransform;
    private string screenType;

    void Start() {
        safeAreaTransform = safeArea.transform;
        safeAreaTransform.GetChild(0).GetComponent<AskForReviewDelegator>().askForReview = gameObject;
    }

    public void PositiveResponse() {

        // They click yes

        // If this is the first response ask for a public review
        if (responseTracker == 0) {
            AnalyticsDelegator.Instance.EnjoyingGame();
            StartCoroutine(RequestForReviews());
        } 
        // Otherwise, send feedback
        else if (responseTracker == 1) {
            
            if (screenType == "Private Feedback") {
                string reason = safeAreaTransform.GetChild(0).GetChild(2).GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>().text;
                AnalyticsDelegator.Instance.NotEnjoyingGame(reason);
                Destroy(gameObject);
            }
        }
    }

    public void NegativeResponse() {

        if (responseTracker == 0) {
            Destroy(safeAreaTransform.GetChild(0).gameObject);
            GameObject newScreen = Instantiate(feedbackScreen);
            newScreen.transform.GetComponent<AskForReviewDelegator>().askForReview = gameObject;
            newScreen.transform.SetParent(safeAreaTransform, false);
            screenType = "Private Feedback";

            responseTracker++;
        }
        else if (responseTracker == 1) {
            Destroy(gameObject);
        }
    }

    private IEnumerator RequestForReviews() {
        #if UNITY_ANDROID
            ReviewManager _reviewManager = new();
            PlayReviewInfo _playReviewInfo;

            var requestFlowOperation = _reviewManager.RequestReviewFlow();
            yield return requestFlowOperation;
            if (requestFlowOperation.Error != ReviewErrorCode.NoError)
            {
                Debug.LogError(requestFlowOperation.Error.ToString());
                yield break;
            }
            _playReviewInfo = requestFlowOperation.GetResult();

            var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
            yield return launchFlowOperation;
            _playReviewInfo = null; // Reset the object
            if (launchFlowOperation.Error != ReviewErrorCode.NoError)
            {
                Debug.LogError(launchFlowOperation.Error.ToString());
                yield break;
            }
        #elif UNITY_IPHONE
            UnityEngine.iOS.Device.RequestStoreReview();
        #endif

        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);

        yield break;
    }
}
