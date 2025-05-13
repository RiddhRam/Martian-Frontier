using UnityEngine;

public class LobbyAdTrigger : MonoBehaviour
{
    private AdDelegator adDelegator;

    void Awake()
    {
        adDelegator = AdDelegator.Instance;
    }

    void OnTriggerEnter2D() {
        adDelegator.ShowLobbyRewardedAd();
    }
}
