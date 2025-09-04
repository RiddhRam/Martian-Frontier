using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RefineryController : MonoBehaviour, IDataPersistence
{
    private static RefineryController _instance;
    public static RefineryController Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<RefineryController>();
            }
            return _instance;
        }
    }

    public Sprite mineEntranceOn;
    public Sprite mineEntranceOff;
    public SpriteRenderer mineEntranceSpriteRenderer;
    public BoxCollider2D mineEntranceBoxCollider;
    
    public GameObject mine;
    public GameObject askForReviewScreen;
    [Header("Progress bars")]
    public Slider refineryProgressSlider;
    public TextMeshProUGUI refineryProgressSliderText;
    
    [Header("Audio")]
    public AudioSource UISoundEffects;
    public AudioSource oreSoundEffects;
    public AudioClip oreSaleSoundEffect;
    public AudioClip batteryRechargeSoundEffect;

    public int refineryTimer;
    public int refineryBattery;
    // 2 Mins
    private const int initialTimer = 120;
    // 450 ores
    private const int initialBattery = 450;
    // The cash made during the current refinery timer, resets to 0 when mine resets
    double cashMadeThisMine;

    private System.Numerics.BigInteger materialsSold;
    public bool askedForReview;

    [SerializeField] private float profitMultiplier = 1;
    private float levelProfitMultiplier = 0;

    public Transform largeFogOfWar;

    [Header("Scripts")]
    public PlayerState playerState;
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

    public void StartRefineryCountdown(int battery = initialBattery) {
        // Mining in progress
        if (countdownCoroutine != null) {
            return;
        }

        countdownCoroutine = StartCoroutine(RefineryCountdown(battery));
    }

    private IEnumerator RefineryCountdown(int battery) {
        // Wait for mine to load before continuing
        // Sometimes refienry controller loads, then starts mine reset, and then mine loads while refinery thinks mine reset
        yield return new WaitUntil(() => mineRenderer.mineInitialization == 2);

        refineryBattery = battery;

        while (refineryBattery > 0)
        {
            UpdateRefineryProgressBars();
            yield return null;
        }

        PlaySaleNoise();

        // Shouldn't be possible but just in case
        if (resetMineCoroutine != null) {
            StopCoroutine(resetMineCoroutine);
        }
        if (increaseBatteryCoroutine != null) {
            StopCoroutine(increaseBatteryCoroutine);
        }

        resetMineCoroutine = StartCoroutine(ResetMine());
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

        if (materialsSold >= 900 && !askedForReview && doneLoading && mineRenderer.mineCount > 1) {
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
        if (cashMadeThisMine != 0)
        {
            Debug.Log(cashMadeThisMine);
            cashMadeThisMine = 0;
        }
        
        NPCManager.Instance.ResetAllNPCPos();
        StartCoroutine(NPCManager.Instance.WaitInLobby());

        // Move player off the dropoff area, and move all players inside the mine to the outside
        playerVehicle.transform.SetPositionAndRotation(new(0, 10, 0), Quaternion.Euler(0, 0, 180));

        doneAnimation = false;
        if (increaseBatteryCoroutine != null) {
            StopCoroutine(increaseBatteryCoroutine);
        }
        increaseBatteryCoroutine = StartCoroutine(GraduallyIncreaseBattery(initialBattery));

        // Destroy all leftover materials, we do it this way, in case someone mined something 
        // just as the mine was shutting down, and the ore didn't have enough time to have 
        // the mine set as its parent
        // This HAS to go first otherwise the mine will not reset tilemaps properly
        yield return mineRenderer.ReturnAllObjectsToPool();

        yield return new WaitUntil(() => doneAnimation);

        // Initialize and uncover map
        mineRenderer.InitializeMine();
        fogOfWarSprite.sortingOrder = 2;

        PostMineReset();
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

        AudioDelegator.Instance.PlayAudio(UISoundEffects, batteryRechargeSoundEffect, 0.35f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            refineryBattery = (int) Mathf.Lerp(0, batteryToUse, elapsed / duration);
            UpdateRefineryProgressBars();
            yield return null; // Wait for the next frame
        }

        // Ensure the final value is exactly the target
        refineryBattery = initialBattery;
        refineryProgressSlider.value = initialBattery;

        doneAnimation = true;
    }

    private void UpdateRefineryProgressBars() {
        refineryProgressSlider.maxValue = initialBattery;
        refineryProgressSlider.value = refineryBattery;

        float percentage = Mathf.Round(refineryBattery * 100f / initialBattery);
        if (percentage == 100 && refineryBattery < initialBattery)
        {
            percentage = 99;
        }

        string barText = $"{percentage}%";

        refineryProgressSliderText.text = barText;
    }

    // Returns the cash added
    public double SellOres(int[] materialsMined)
    {
        // Track number of ores mined and cash earned
        int change = 0;
        double cashToAdd = 0;

        for (int i = 0; i != mineRenderer.oreDelegation.GetOriginalMaterialPrices().Length; i++)
        {

            if (materialsMined[i] <= 0 || refineryBattery <= 0)
            {
                continue;
            }

            int itemsSold = 0;

            while (materialsMined[i] > 0 && refineryBattery > 0)
            {
                itemsSold++;
                materialsMined[i]--;
                refineryBattery--;
            }

            cashToAdd += RefineryUpgradePad.Instance.GetActualMaterialPrice(i) * itemsSold;
            change += itemsSold;
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

    public int GetInitialBattery()
    {
        return initialBattery;
    }

    public void LoadData(GameData data)
    {
        if (!data.finishedTutorial)
        {
            firstTimePlaying = true;
        }

        // Just gaurantee that the player can enter the mine
        mineEntranceSpriteRenderer.sprite = mineEntranceOn;
        mineEntranceBoxCollider.isTrigger = true;

        this.materialsSold = System.Numerics.BigInteger.Parse(data.materialsSold);
        this.askedForReview = data.askedForReview;

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op"))
        {
            notSinglePlayerScene = true;
            return;
        }

        if (resetMineCoroutine != null)
        {
            StopCoroutine(resetMineCoroutine);
        }
        if (increaseBatteryCoroutine != null)
        {
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

    public float GetAspectValue(bool alternate = false)
    {
        float alternateMultiplier = 1.1f;
        float multiplier = 0.75f;
        float multiplierToUse = multiplier;
        if (alternate)
        {
            multiplierToUse = alternateMultiplier;
        }
        // Determine the scale to set the refinery panel to

        // Min and max aspect ratios
        const float MinAspect = 1f;            // 1:1
        const float MaxAspect = 16 / 9f;      // 9:16

        // Min and max output values
        const float MinValue = 0.7f;
        const float MaxValue = 1f;
        
        float aspect = (float)Screen.height / Screen.width;
        
        if (aspect <= MinAspect)
        {
            return MinValue * multiplierToUse;
        }

        if (aspect >= MaxAspect)
        {
            return MaxValue * multiplierToUse;
        }
            
        // Linear interpolation between MinValue and MaxValue
        float t = (aspect - MinAspect) / (MaxAspect - MinAspect);
        return Mathf.Lerp(MinValue, MaxValue, t) * multiplierToUse;
    }

    public void SetProfitMultiplier(float newMultiplier)
    {
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