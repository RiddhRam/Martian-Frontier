using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyAd : MonoBehaviour
{
    public AdDelegator adDelegator;

    void OnTriggerEnter2D() {
        adDelegator.ShowLobbyRewardedAd();
    }
}
