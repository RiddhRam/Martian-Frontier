using UnityEngine;

public class PrivacyPolicy : MonoBehaviour
{
    public GameObject text;
    public GameObject content;

    public void ShowPolicy() {
        gameObject.SetActive(true);
    }

    public void HidePolicy() {
        gameObject.SetActive(false);
    }
}
