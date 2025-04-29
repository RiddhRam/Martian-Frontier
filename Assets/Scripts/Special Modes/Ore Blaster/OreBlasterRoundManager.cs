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

    [SerializeField] TextMeshProUGUI creditTextAmount;
    [SerializeField] TextMeshProUGUI gemTextAmount;

    [SerializeField] PlayerState playerState;
    [SerializeField] MineRenderer mineRenderer;
    [SerializeField] OreBlaster oreBlaster;

    [SerializeField] private int roundTimer;
    public bool roundInProgress = false;

    [SerializeField] private AudioSource UISoundEffects;
    [SerializeField] private AudioClip roundEndSoundEffect;
    [SerializeField] private AudioDelegator audioDelegator;

    void Start()
    {
        oreBlaster.UpdateCreditCount(0);
        StartCoroutine(AnimateArrow());
        UpdateConversion();
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
        
        audioDelegator.PlayAudio(UISoundEffects, roundEndSoundEffect, 0.4f);
        Debug.Log(oreBlaster.collectedCredits);
        playerState.AddCredits(oreBlaster.collectedCredits);
        UpdateConversion();
        // Remove all credits
        oreBlaster.UpdateCreditCount(-oreBlaster.collectedCredits);

        roundInfo.SetActive(false);
        roundInProgress = false;
    }

    public void UpdateConversion() {
        creditTextAmount.text = playerState.FormatPrice(playerState.GetUserCredits());
        gemTextAmount.text = playerState.FormatPrice((int) (playerState.GetUserCredits() / 30));
    }

    private IEnumerator ResetMine() {
        yield return mineRenderer.ReturnAllObjectsToPool();
        mineRenderer.InitializeMine();
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