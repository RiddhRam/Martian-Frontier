using System.Collections;
using TMPro;
using UnityEngine;

// For Magnet Hauler, also known as Ore Magnet
public class OreMagnetRoundManager : MonoBehaviour
{
    [SerializeField] GameObject largeFogOfWar;
    [SerializeField] Transform playerVehicle;
    [SerializeField] RectTransform enterMineArrow;

    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] GameObject roundInfo;

    [SerializeField] CreditsDelegator creditsDelegator;
    [SerializeField] PlayerState playerState;
    [SerializeField] MagnetHauler magnetHauler;
    [SerializeField] OreMagnetUpgrades oreMagnetUpgrades;
    [SerializeField] MagnetHaulerDailyChallengeDelegator magnetHaulerDailyChallengeDelegator;

    [SerializeField] private int roundTimer;
    public bool roundInProgress = false;

    [SerializeField] private AudioSource UISoundEffects;
    [SerializeField] private AudioClip roundEndSoundEffect;


    void Start()
    {
        magnetHauler.UpdateCreditCount(0);
        StartCoroutine(AnimateArrow());
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (roundInProgress) {
            return;
        }
        
        StartCoroutine(RoundCountdown());
    }

    private IEnumerator RoundCountdown() {
        roundInProgress = true;
        roundInfo.SetActive(true);
        enterMineArrow.gameObject.SetActive(false);
        // Let player see the map
        largeFogOfWar.SetActive(false);

        roundTimer = 30;
        
        while (roundTimer > 0) {
            timerText.text = roundTimer.ToString() + "s";
            roundTimer--;
            yield return new WaitForSeconds(1);
        }

        playerVehicle.position = new(0, 5);
        enterMineArrow.gameObject.SetActive(true);
        // Hide map
        largeFogOfWar.SetActive(true);
        
        AudioDelegator.Instance.PlayAudio(UISoundEffects, roundEndSoundEffect, 0.25f);

        playerState.AddCredits(magnetHauler.collectedCredits);
        magnetHaulerDailyChallengeDelegator.CollectedCredits(magnetHauler.collectedCredits);
        // Remove all credits
        magnetHauler.UpdateCreditCount(-magnetHauler.collectedCredits);

        roundInfo.SetActive(false);
        roundInProgress = false;

        oreMagnetUpgrades.EnableNoticeIconIfNeeded();

        ResetAllCreditMaterials();
    }

    public void ResetAllCreditMaterials() {
        Transform creditsDelegatorTransform = creditsDelegator.transform;

        // Return all objects
        for (int i = 0; i != creditsDelegatorTransform.childCount; i++) {
            if (creditsDelegatorTransform.GetChild(i).gameObject.activeSelf) {
                creditsDelegator.ReturnCreditGameObject(creditsDelegatorTransform.GetChild(i).gameObject);
            }
        }

        // Place them again
        creditsDelegator.GenerateCredits();
    }

    private IEnumerator AnimateArrow() {

        // Save the original position for reference
        Vector2 originalPos = enterMineArrow.anchoredPosition;

        float speed = 3f;      // Controls the speed of the oscillation

        while (true) {
            // Calculate the new y offset using Mathf.Sin
            float offsetY = Mathf.Sin(Time.time * speed) * 2;
            
            // Update the anchored position while preserving the x-coordinate
            enterMineArrow.anchoredPosition = new Vector2(originalPos.x, originalPos.y + offsetY);
            
            // Wait until the next frame
            yield return null;
        }
    }

}