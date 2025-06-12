using UnityEngine;

public class LobbyAdTrigger : MonoBehaviour
{
    void OnTriggerEnter2D() {
        AdDelegator.Instance.ShowLobbyRewardedAd();
    }
}
