using System.Collections;
using TMPro;
using UnityEngine;

// For Ore Blaster
public class OreBlasterRoundManager : MonoBehaviour
{

    [SerializeField] Transform playerVehicle;
    [SerializeField] RectTransform enterMineArrow;

    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] GameObject roundInfo;

    [SerializeField] PlayerState playerState;
    [SerializeField] MineRenderer mineRenderer;
    [SerializeField] OreBlaster oreBlaster;
    [SerializeField] OreBlasterUpgrades oreBlasterUpgrades;
    [SerializeField] OreBlasterDailyChallengeDelegator oreBlasterDailyChallengeDelegator;

    [SerializeField] private int roundTimer;
    public bool roundInProgress = false;

    [SerializeField] private AudioSource UISoundEffects;
    [SerializeField] private AudioClip roundEndSoundEffect;

    void Start()
    {        
        oreBlaster.UpdateCreditCount(0);
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
 
        roundTimer = 30;
        
        while (roundTimer > 0) {
            timerText.text = roundTimer.ToString() + "s";
            roundTimer--;
            yield return new WaitForSeconds(1);
        }

        playerVehicle.position = new(0, 5);
        enterMineArrow.gameObject.SetActive(true);

        StartCoroutine(ResetMine());
        
        AudioDelegator.Instance.PlayAudio(UISoundEffects, roundEndSoundEffect, 0.25f);

        playerState.AddCredits(oreBlaster.collectedCredits);
        oreBlasterDailyChallengeDelegator.BlastedCredits(oreBlaster.collectedCredits);
        // Remove all credits and ores
        oreBlaster.collectedCredits = 0;

        roundInfo.SetActive(false);
        roundInProgress = false;

        oreBlasterUpgrades.EnableNoticeIconIfNeeded();
    }


    private IEnumerator ResetMine() {
        mineRenderer.mineInitialization = 0;
        yield return mineRenderer.ReturnAllObjectsToPool();
        mineRenderer.InitializeMine();
        mineRenderer.mineInitialization = 2;
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