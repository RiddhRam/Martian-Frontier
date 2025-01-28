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
    public GameObject[] secondScreens;
    private Transform safeAreaTransform;
    private string screenType;

    void Start() {
        safeAreaTransform = safeArea.transform;
        safeAreaTransform.GetChild(0).GetComponent<AskForReviewDelegator>().askForReview = gameObject;
    }

    public void PositiveResponse() {

        if (responseTracker == 0) {
            Destroy(safeAreaTransform.GetChild(0).gameObject);
            GameObject newScreen = Instantiate(secondScreens[0]);
            newScreen.transform.GetComponent<AskForReviewDelegator>().askForReview = gameObject;
            newScreen.transform.SetParent(safeAreaTransform, false);
            screenType = "Public Review";

            responseTracker++;
        } else if (responseTracker == 1) {
            
            if (screenType == "Private Feedback") {
                string reason = safeAreaTransform.GetChild(0).GetChild(2).GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>().text;
                GameObject.Find("Analytics Delegator").GetComponent<AnalyticsDelegator>().NotEnjoyingGame(reason);
                Destroy(gameObject);
            } else if (screenType == "Public Review") {
                StartCoroutine(RequestForReviews());
                GameObject.Find("Analytics Delegator").GetComponent<AnalyticsDelegator>().EnjoyingGame();
            }
            
        }
    }

    public void NegativeResponse() {

        if (responseTracker == 0) {
            Destroy(safeAreaTransform.GetChild(0).gameObject);
            GameObject newScreen = Instantiate(secondScreens[1]);
            newScreen.transform.GetComponent<AskForReviewDelegator>().askForReview = gameObject;
            newScreen.transform.SetParent(safeAreaTransform, false);
            screenType = "Private Feedback";

            responseTracker++;
        }
        else if (responseTracker == 1) {
            Destroy(gameObject);
        }
    }

    public void Test() {
        StartCoroutine(RequestForReviews());
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
