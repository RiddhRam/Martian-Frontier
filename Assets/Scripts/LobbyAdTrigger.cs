using UnityEngine;

public class LobbyAdTrigger : MonoBehaviour
{
    [SerializeField] private AdDelegator adDelegator;

    void OnTriggerEnter2D() {
        adDelegator.ShowLobbyRewardedAd();
    }
}
