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

    [SerializeField] private TextMeshProUGUI cashMadeThisMineText;
    
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
    float cashMadeThisMine;

    private System.Numerics.BigInteger materialsSold;
    public bool askedForReview;

    [SerializeField] private int[] materialPrices;
    [SerializeField] private float profitMultiplier = 1;
    private float levelProfitMultiplier = 0;

    public Transform largeFogOfWar;

    private AudioDelegator audioDelegator;
    private DataPersistenceManager dataPersistenceManager;
    public GameObject playerVehicle;
    private AnalyticsDelegator analyticsDelegator;
    public MineRenderer mineRenderer;
    public TutorialManager tutorialManager;
    public NPCManager nPCManager;
    public UpgradesDelegator upgradesDelegator;
    [SerializeField] PlayerMovement playerMovement;

    private bool doneLoading = false;
    bool doneAnimation;
    public SpriteRenderer fogOfWarSprite;
    private AdDelegator adDelegator;

    private Coroutine resetMineCoroutine;
    private Coroutine increaseBatteryCoroutine;
    private Coroutine countdownCoroutine;

    private bool firstTimePlaying = false;
    private bool notSinglePlayerScene = false;

    void Awake() {
        adDelegator = AdDelegator.Instance;
        audioDelegator = AudioDelegator.Instance;
        dataPersistenceManager = DataPersistenceManager.Instance;
        analyticsDelegator = AnalyticsDelegator.Instance;
        
        materialPrices = mineRenderer.GetComponent<OreDelegation>().GetMaterialPrices();
    }

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

        if (notSinglePlayerScene) {
            nPCManager.ResetPlayerPos();
            nPCManager.ResetAllNPCPos();
        }
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

        UpdateCashText();

        if (materialsSold >= 1000 && !askedForReview && doneLoading) {
            askedForReview = true;
            askForReviewScreen.SetActive(true);
            
            analyticsDelegator.ContinuedAfterTutorial();

        } else if (askedForReview) {
            Destroy(askForReviewScreen);
        }

        // If there is a lobby ad display added to ad delegator, try to show the lobby ad reward
        if (adDelegator.lobbyAdDisplay) {
            StartCoroutine(adDelegator.TryShowLobbyReward((long) cashMadeThisMine));
        }

        playerState.UpdateHighestMined(cashMadeThisMine);
        cashMadeThisMine = 0;

        if (nPCManager) {
            StartCoroutine(nPCManager.WaitInLobby());
        }

        // Move player off the dropoff area, and move all players inside the mine to the outside
        playerVehicle.transform.SetPositionAndRotation(new(0, 3, 0), Quaternion.Euler(0, 0, 180));

        doneAnimation = false;
        if (increaseBatteryCoroutine != null) {
            StopCoroutine(increaseBatteryCoroutine);
        }
        increaseBatteryCoroutine = StartCoroutine(GraduallyIncreaseBattery(initialTimer));

        mineRenderer.currentOresMined = 0;
        mineRenderer.oresMinedText.text = "0";

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

        audioDelegator.PlayAudio(UISoundEffects, batteryRechargeSoundEffect, 0.45f);

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

    public void SellOres(int[] materialsMined, bool isNPC) {
        // Track number of ores mined and cash earned
        int change = 0;
        long cashToAdd = 0;

        for (int i = 0; i != materialPrices.Length; i++) {
            cashToAdd += materialPrices[i] * materialsMined[i];
            change += materialsMined[i];
        }

        // Update stats
        materialsSold += change;
        playerState.NewMaterialsSold(change, isNPC);

        // Should never be less than 0
        if (cashToAdd <= 0) {
            return;
        }

        // Add cash
        cashToAdd = (long) (cashToAdd * GetTotalProfitMultiplier());
        cashMadeThisMine += cashToAdd;

        playerState.AddCash(cashToAdd, true);
        playerMovement.NewOreMined(cashToAdd);
        UpdateCashText();

        if (tutorialManager == null) {
            return;
        }
    }

    private void UpdateCashText() {
        cashMadeThisMineText.text = "$" + playerState.FormatPrice((long) cashMadeThisMine);
    }

    public void PlaySaleNoise() {
        audioDelegator.PlayAudio(oreSoundEffects, oreSaleSoundEffect, 0.4f);
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

        this.refineryTimer = data.refineryTimer;
        
        // Two cases when opening the game
        // Case 1: Player left game while mine was resetting, or never initialized, so just reset the mine properly this time
        if (data.mineInitialization == 0) {
            resetMineCoroutine = StartCoroutine(ResetMine());
        } 
        // Case 2: Player left the game while refinery timer was counting down, so continue countdown
        else if (refineryTimer != initialTimer) {
            StartRefineryCountdown(refineryTimer);
        }

        UpdateRefineryProgressBars();
       
        doneLoading = true;
    }

    public void SaveData(ref GameData data) {
        data.materialsSold = this.materialsSold.ToString();
        data.askedForReview = this.askedForReview;

        if (notSinglePlayerScene) {
            return;
        }

        data.refineryTimer = this.refineryTimer;
    }

    private void SaveGame() {
        dataPersistenceManager.SaveGame();
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
        // Have to round due to floating point errors
        float multiplier = profitMultiplier + levelProfitMultiplier + upgradesDelegator.profitMultiplier;

        return Mathf.Round(multiplier * 100f) / 100f;
    }

}