using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AskForReviewDelegator : MonoBehaviour
{

    public GameObject askForReview;

    public void SendPositiveResponse() {
        askForReview.GetComponent<AskForReview>().PositiveResponse();
    }

    public void SendNegativeResponse() {
        askForReview.GetComponent<AskForReview>().NegativeResponse();
    }

}
