using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RefineryController : MonoBehaviour, IDataPersistence
{
    public Sprite mineEntranceOn;
    public Sprite mineEntranceOff;
    public SpriteRenderer mineEntranceSpriteRenderer;
    public BoxCollider2D mineEntranceBoxCollider;
    
    public GameObject mine;
    public GameObject[] refineryProgressSliders;
    public GameObject[] refineryProgressSlidersText;
    public PlayerState playerState;
    public GameObject askForReviewScreen;

    public AudioSource UISoundEffects;
    public AudioSource oreSoundEffects;
    public AudioClip oreSaleSoundEffect;
    public AudioClip batteryRechargeSoundEffect;

    public int refineryTimer;
    // 5 Mins
    private const int initialTimer = 120;
    // The cash made during the current refinery timer, resets to 0 when mine resets
    double cashMadeThisMine;

    private System.Numerics.BigInteger materialsSold;
    public bool askedForReview;

    [SerializeField] private float profitMultiplier = 1;
    private float levelProfitMultiplier = 0;

    public Transform largeFogOfWar;

    public GameObject playerVehicle;
    public MineRenderer mineRenderer;
    public TutorialManager tutorialManager;
    public UpgradesDelegator upgradesDelegator;
    [SerializeField] PlayerMovement playerMovement;

    public bool doneLoading = false;
    bool doneAnimation;
    public SpriteRenderer fogOfWarSprite;

    private Coroutine resetMineCoroutine;
    private Coroutine increaseBatteryCoroutine;
    public Coroutine countdownCoroutine;

    private bool firstTimePlaying = false;
    private bool notSinglePlayerScene = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Even if the mine entrance collider is set to not be a trigger, the drill attached to the drillers is still a trigger
        // So this function is called even while mine is restting
        // Make sure the mine entrance is not closed before starting
        if (!mineEntranceBoxCollider.isTrigger) {
            return;
        }

        StartRefineryCountdown(initialTimer);
    }

    public void StartRefineryCountdown(int timer = initialTimer) {
        // Mining in progress
        if (countdownCoroutine != null) {
            return;
        }

        countdownCoroutine = StartCoroutine(RefineryCountdown(timer));
    }

    private IEnumerator RefineryCountdown(int timer) {
        // Wait for mine to load before continuing
        // Sometimes refienry controller loads, then starts mine reset, and then mine loads while refinery thinks mine reset
        yield return new WaitUntil(() => mineRenderer.mineInitialization == 2);

        refineryTimer = timer;

        while (refineryTimer > 0) {
            UpdateRefineryProgressBars();
            yield return new WaitForSecondsRealtime(1f);
            refineryTimer--;
        }

        PlaySaleNoise();

        // Shouldnt be possible but just in case
        if (resetMineCoroutine != null) {
            StopCoroutine(resetMineCoroutine);
        }
        if (increaseBatteryCoroutine != null) {
            StopCoroutine(increaseBatteryCoroutine);
        }

        resetMineCoroutine = StartCoroutine(ResetMine());
        NPCManager.Instance.ResetAllNPCPos();
    }

    public void CallResetMineFromButton() {
    
        if (resetMineCoroutine != null) {
            StopCoroutine(resetMineCoroutine);
        }
        if (increaseBatteryCoroutine != null) {
            StopCoroutine(increaseBatteryCoroutine);
        }
        resetMineCoroutine = StartCoroutine(ResetMine());
    }

    private IEnumerator ResetMine() {
        if (countdownCoroutine != null) {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        mineRenderer.mineInitialization = 0;

        // Reset mine
        // Stop user from entering mine
        mineEntranceBoxCollider.isTrigger = false;
        mineEntranceSpriteRenderer.sprite = mineEntranceOff;

        if (materialsSold >= 1000 && !askedForReview && doneLoading) {
            askedForReview = true;
            askForReviewScreen.SetActive(true);
            
            AnalyticsDelegator.Instance.ContinuedAfterTutorial();

        } else if (askedForReview) {
            Destroy(askForReviewScreen);
        }

        // If there is a lobby ad display added to ad delegator, try to show the lobby ad reward
        if (AdDelegator.Instance.lobbyAdDisplay) {
            StartCoroutine(AdDelegator.Instance.TryShowLobbyReward(cashMadeThisMine));
        }

        playerState.UpdateHighestMined(cashMadeThisMine);
        cashMadeThisMine = 0;

        StartCoroutine(NPCManager.Instance.WaitInLobby());

        // Move player off the dropoff area, and move all players inside the mine to the outside
        playerVehicle.transform.SetPositionAndRotation(new(0, 10, 0), Quaternion.Euler(0, 0, 180));

        doneAnimation = false;
        if (increaseBatteryCoroutine != null) {
            StopCoroutine(increaseBatteryCoroutine);
        }
        increaseBatteryCoroutine = StartCoroutine(GraduallyIncreaseBattery(initialTimer));

        // Destroy all leftover materials, we do it this way, in case someone mined something 
        // just as the mine was shutting down, and the ore didn't have enough time to have 
        // the mine set as its parent
        // This HAS to go first otherwise the mine will not reset tilemaps properly
        yield return mineRenderer.ReturnAllObjectsToPool();

        yield return new WaitUntil(() => doneAnimation);

        // Initialize and uncover map
        mineRenderer.InitializeMine();
        fogOfWarSprite.sortingOrder = 3;

        PostMineReset();

        SaveGame();
    }

    public void PostMineReset() {
        // Wait for this to be done
        mineRenderer.mineInitialization = 2;
        
        // Renable the mine
        mineEntranceSpriteRenderer.sprite = mineEntranceOn;
        mineEntranceBoxCollider.isTrigger = true;
        mineEntranceBoxCollider.enabled = false;
        mineEntranceBoxCollider.enabled = true;
    }

    private IEnumerator GraduallyIncreaseBattery(float batteryToUse)
    {
        // Cover the map
        largeFogOfWar.position = new(0, -256, 0);
        fogOfWarSprite.sortingOrder = 6;

        float duration = 6.0f; // Duration of the increase in seconds
        float elapsed = 0f;

        AudioDelegator.Instance.PlayAudio(UISoundEffects, batteryRechargeSoundEffect, 0.45f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            refineryTimer = (int) Mathf.Lerp(0, batteryToUse, elapsed / duration);
            UpdateRefineryProgressBars();
            yield return null; // Wait for the next frame
        }

        // Ensure the final value is exactly the target
        refineryTimer = initialTimer;

        for (int i = 0; i != refineryProgressSliders.Length; i++) {
            refineryProgressSliders[i].GetComponent<Slider>().value = refineryTimer;
        }

        doneAnimation = true;
    }

    private void UpdateRefineryProgressBars() {

        for (int i = 0; i != refineryProgressSliders.Length; i++) {
            refineryProgressSliders[i].GetComponent<Slider>().maxValue = initialTimer;
            refineryProgressSliders[i].GetComponent<Slider>().value = refineryTimer;
        }

        int minutes = refineryTimer / 60;
        int seconds = refineryTimer % 60;

        // Round up to nearest int
        string barText = $"{minutes}:{seconds:D2}";

        for (int i = 0; i != refineryProgressSlidersText.Length; i++) {
            refineryProgressSlidersText[i].GetComponent<TextMeshProUGUI>().text = barText;
        }
    }

    // Returns the cash added
    public double SellOres(int[] materialsMined)
    {
        // Track number of ores mined and cash earned
        int change = 0;
        double cashToAdd = 0;

        for (int i = 0; i != mineRenderer.oreDelegation.GetOriginalMaterialPrices().Length; i++)
        {

            if (materialsMined[i] <= 0)
            {
                continue;
            }

            cashToAdd += mineRenderer.refineryUpgradePad.GetActualMaterialPrice(i) * materialsMined[i];
            change += materialsMined[i];
        }

        // Update stats
        materialsSold += change;
        playerState.NewMaterialsSold(change);

        // Should never be less than 0
        if (cashToAdd <= 0)
        {
            return 0;
        }

        // Add cash
        cashToAdd = (long)(cashToAdd * GetTotalProfitMultiplier());
        cashMadeThisMine += cashToAdd;

        playerState.AddCash(cashToAdd, true);

        return cashToAdd;
    }

    public void PlaySaleNoise() {
        AudioDelegator.Instance.PlayAudio(oreSoundEffects, oreSaleSoundEffect, 0.4f);
    }

    public int GetRefineryTimer() {
        return refineryTimer;
    }

    public int GetInitialTimer() {
        return initialTimer;
    }

    public void LoadData(GameData data) {
        if (!data.finishedTutorial) {
            firstTimePlaying = true;
        }

        // Just gaurantee that the player can enter the mine
        mineEntranceSpriteRenderer.sprite = mineEntranceOn;
        mineEntranceBoxCollider.isTrigger = true;

        this.materialsSold = System.Numerics.BigInteger.Parse(data.materialsSold);
        this.askedForReview = data.askedForReview;

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            notSinglePlayerScene = true;
            return;
        }
        
        if (resetMineCoroutine != null) {
            StopCoroutine(resetMineCoroutine);
        }
        if (increaseBatteryCoroutine != null) {
            StopCoroutine(increaseBatteryCoroutine);
        }
        
        StartCoroutine(ResetMine());
        UpdateRefineryProgressBars();
       
        doneLoading = true;
    }

    public void SaveData(ref GameData data) {
        data.materialsSold = this.materialsSold.ToString();
        data.askedForReview = this.askedForReview;
    }

    private void SaveGame() {
        DataPersistenceManager.Instance.SaveGame();
    }

    public void SetProfitMultiplier(float newMultiplier) {
        profitMultiplier = newMultiplier;
    }

    public float GetProfitMultiplier() {
        return profitMultiplier;
    }

    public void SetLevelProfitMultiplier(float newLevelMultiplier) {
        // Have to round due to floating point errors
        levelProfitMultiplier = Mathf.Round(newLevelMultiplier * 100f) / 100f;
    }

    public float GetLevelProfitMultiplier() {
        return levelProfitMultiplier;
    }

    public float GetProfitBoostMultiplier() {
        return upgradesDelegator.profitMultiplier;
    }

    public float GetTotalProfitMultiplier() {

        // Not currently using levelProfitMultiplier
        //float multiplier = profitMultiplier + levelProfitMultiplier + upgradesDelegator.profitMultiplier;
        float multiplier = profitMultiplier + upgradesDelegator.profitMultiplier;

        // Have to round due to floating point errors
        return Mathf.Round(multiplier * 100f) / 100f;
    }

}