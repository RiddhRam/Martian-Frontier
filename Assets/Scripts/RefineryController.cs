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
    public BoxCollider2D gameObjectBoxCollider2D;
    public GameObject mine;
    public GameObject[] refineryProgressSliders;
    public GameObject[] refineryProgressSlidersText;
    public PlayerState playerState;
    public GameObject askForReviewScreen;
    public AudioSource vehicleSoundEffects;
    public AudioSource UISoundEffects;
    public AudioClip oreSaleSoundEffect;
    public AudioClip batteryRechargeSoundEffect;

    [SerializeField]
    private float refineryBattery;
    [SerializeField]
    private float initialBattery;
    private System.Numerics.BigInteger materialsSold;
    public bool askedForReview;
    private int[] materialPrices;
    public GameObject capacityUpgrades;
    [SerializeField]
    private float profitMultiplier = 1;
    private float levelProfitMultiplier = 0;
    [SerializeField]
    private float rebirthProfitMultiplier = 0;
    public Transform largeFogOfWar;
    public AudioDelegator audioDelegator;
    public DataPersistenceManager dataPersistenceManager;
    public GameObject playerVehicle;
    public AnalyticsDelegator analyticsDelegator;
    public MineRenderer mineRenderer;
    public DailyChallengeDelegator dailyChallengeDelegator;
    public TutorialManager tutorialManager;
    public NPCManager nPCManager;

    private bool doneLoading = false;
    bool doneAnimation;
    public SpriteRenderer fogOfWarSprite;
    public AdDelegator adDelegator;
    private Coroutine resetMineCoroutine;
    private Coroutine increaseBatteryCoroutine;
    private bool firstTimePlaying = false;
    private bool notSinglePlayerScene = false;

    void Awake() {
        materialPrices = GameObject.Find("Ore Prices").GetComponent<OreDelegation>().GetMaterialPrices();
    }

    void Start() {
        analyticsDelegator = AnalyticsDelegator.Instance;
        if (initialBattery < refineryBattery || initialBattery < 120) {
            initialBattery = 120;
        }

        if (refineryBattery > initialBattery) {
            refineryBattery = initialBattery;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        HaulerController haulerController = collision.gameObject.GetComponent<HaulerController>();

        // Make sure it was a hauler that collides, only haulers will have a HaulerController
        if (!haulerController) {
            return;
        }
        
        int[] materialCount = haulerController.GetMaterialCount();
        float[] materialProfitMultipliers = haulerController.GetMaterialProfitMultipliers();

        int preSale = haulerController.GetTotalMaterialCount();

        // Track what's being added so we can verify the cash amount
        int[] savedMaterialCount = new int[materialCount.Length];

        // Refinery each ore by reducing refinery battery and adding money to user's account
        for (int i = 0; i != materialCount.Length; i++) {
            // Have to start j in the negative because the values will meet in the middle at 0
            // j increases by 1, but materialCount[i] also decreases by 1
            for (int j = -materialCount[i]; j < materialCount[i]; j++) {
                if (refineryBattery <= 0) {
                    break;
                }
                
                refineryBattery -= 1;
                materialCount[i]--;
                playerState.NewMaterialSold();
                savedMaterialCount[i]++;
            }
        }

        materialsSold += preSale - haulerController.GetTotalMaterialCount();
        dailyChallengeDelegator.SoldOres(preSale - haulerController.GetTotalMaterialCount());

        if (materialsSold >= 20 && !askedForReview && doneLoading) {
            askedForReview = true;
            askForReviewScreen.SetActive(true);
        } else if (askedForReview) {
            Destroy(askForReviewScreen);
        }

        // Reset the mine if needed
        if (refineryBattery <= 0) {
            // Stop user from entering user dropoff or mine
            gameObject.GetComponent<BoxCollider2D>().isTrigger = false;

            // Shouldnt be possible
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

        UpdateRefineryProgressBars();

        // Calculate how much money to add
        long cashToAdd = 0;
        for (int i = 0; i != savedMaterialCount.Length; i++) {
            cashToAdd += (long) (savedMaterialCount[i] * materialPrices[i] * (1 + materialProfitMultipliers[i]));
        }

        // Should never be less than 0
        if (cashToAdd <= 0) {
            return;
        }

        cashToAdd = (long) (cashToAdd * GetTotalProfitMultiplier() * (1 + haulerController.GetProfitMultiplier()));

        // Verify that this is the right amount
        playerState.AddCash(cashToAdd, haulerController.CheckIfNpc());
        haulerController.SetMaterialCount(materialCount);
        haulerController.ShowFloatingText("$" + FormatPrice(cashToAdd));
        audioDelegator.PlayAudio(vehicleSoundEffects, oreSaleSoundEffect, 0.4f);

        if (!haulerController.CheckIfNpc()) {
            analyticsDelegator.DropOffOres(collision.name, haulerController.GetTotalMaterialCount(), cashToAdd);
        } else {
            collision.transform.parent.GetComponent<NPCMovement>().AskIfHaulingIsNeeded();
        }

        if (tutorialManager == null) {
            return;
        }

        if (firstTimePlaying && tutorialManager.finishedTutorial) {
            analyticsDelegator.ContinuedAfterTutorial();
            firstTimePlaying = false;
        }
    }

    public void PlaySaleNoise() {
        audioDelegator.PlayAudio(vehicleSoundEffects, oreSaleSoundEffect, 0.4f);
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
        mineRenderer.mineInitialization = 0;

        // Disable drop offs while resetting
        gameObjectBoxCollider2D.isTrigger = false;

        // Disable mine temporarily
        mineEntranceSpriteRenderer.sprite = mineEntranceOff;
        mineEntranceBoxCollider.enabled = true;

        // Move player off the dropoff area, and move all players inside the mine to the outside
        playerVehicle.transform.SetPositionAndRotation(new(4.5f, 5.4f, 0), Quaternion.Euler(0, 0, 180));

        doneAnimation = false;
        if (increaseBatteryCoroutine != null) {
            StopCoroutine(increaseBatteryCoroutine);
        }
        increaseBatteryCoroutine = StartCoroutine(GraduallyIncreaseBattery(initialBattery));

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
        mineEntranceBoxCollider.enabled = false;

        // Let user use dropoff, also flash it in case user was anxiously trying to use it by pressing against it
        gameObjectBoxCollider2D.isTrigger = true;
        gameObjectBoxCollider2D.enabled = false;
        gameObjectBoxCollider2D.enabled = true;

        refineryBattery = initialBattery;
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
            refineryBattery = (int) Mathf.Lerp(0, batteryToUse, elapsed / duration);
            UpdateRefineryProgressBars();
            yield return null; // Wait for the next frame
        }

        // Ensure the final value is exactly the target
        refineryBattery = initialBattery;

        for (int i = 0; i != refineryProgressSliders.Length; i++) {
            refineryProgressSliders[i].GetComponent<Slider>().value = refineryBattery;
        }

        for (int i = 0; i != refineryProgressSlidersText.Length; i++) {
            refineryProgressSlidersText[i].GetComponent<TextMeshProUGUI>().text = "100%";
        }

        doneAnimation = true;
    }

    private void UpdateRefineryProgressBars() {

        for (int i = 0; i != refineryProgressSliders.Length; i++) {
            refineryProgressSliders[i].GetComponent<Slider>().maxValue = initialBattery;
            refineryProgressSliders[i].GetComponent<Slider>().value = refineryBattery;
        }

        // Round up to nearest int
        string barText = Mathf.CeilToInt(refineryBattery * 100 / initialBattery) + "%";
        // Unless it's at 99, then don't round up to 100
        if (Mathf.FloorToInt(refineryBattery * 100 / initialBattery) == 99) {
            barText = "99%";
        }

        for (int i = 0; i != refineryProgressSlidersText.Length; i++) {
            refineryProgressSlidersText[i].GetComponent<TextMeshProUGUI>().text = barText;
        }
    }

    public void SetBattery(float newValue) {
        refineryBattery = newValue - (initialBattery - refineryBattery);
        initialBattery = newValue;
        SaveGame();
        UpdateRefineryProgressBars();
    }

    public void SetRefineryBattery(float newValue) {
        refineryBattery = newValue;
        UpdateRefineryProgressBars();
    }

    public float GetRefineryBattery() {
        return refineryBattery;
    }

    public float GetInitialBattery() {
        return initialBattery;
    }

    public void LoadData(GameData data) {
        if (!data.finishedTutorial) {
            firstTimePlaying = true;
        }

        mineEntranceSpriteRenderer.sprite = mineEntranceOn;
        mineEntranceBoxCollider.enabled = false;

        this.materialsSold = System.Numerics.BigInteger.Parse(data.materialsSold);
        this.askedForReview = data.askedForReview;

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            notSinglePlayerScene = true;

            this.initialBattery = 750;
            this.refineryBattery = 750;

            return;
        }
        
        if (resetMineCoroutine != null) {
            StopCoroutine(resetMineCoroutine);
        }
        if (increaseBatteryCoroutine != null) {
            StopCoroutine(increaseBatteryCoroutine);
        }

        this.initialBattery = 200;
        this.refineryBattery = data.refineryBattery;

        if (this.refineryBattery > this.initialBattery) {
            this.refineryBattery = this.initialBattery;
        }

        // If refinery controller bar was in reset animation, then skip it and go straight to 100%
        if (data.mineInitialization == 0) {
            resetMineCoroutine = StartCoroutine(ResetMine());
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

        data.refineryBattery = this.refineryBattery;
    }

    private void SaveGame() {
        dataPersistenceManager.SaveGame();
    }

    private string FormatPrice(long price)
    {
        if (price >= 1_000_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Qu"
            return (Mathf.Floor(price / 1_000_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "Qu";
        }
        else if (price >= 1_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "Q"
            return (Mathf.Floor(price / 1_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "Q";
        }
        else if (price >= 1_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "T"
            return (Mathf.Floor(price / 1_000_000_000_000f * 1000) / 1000).ToString("0.###") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            // Truncate to 3 decimal places and format with "B"
            return (Mathf.Floor(price / 1_000_000_000f * 1000) / 1000).ToString("0.###") + "B";
        }
        else if (price >= 1_000_000)
        {
            // Truncate to 3 decimal places and format with "M"
            return (Mathf.Floor(price / 1_000_000f * 1000) / 1000).ToString("0.###") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 3 decimal places and format with "K"
            return (Mathf.Floor(price / 1_000f * 1000) / 1000).ToString("0.###") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
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

    public void SetRebirthProfitMultiplier(float newRebirthMultiplier) {
        // Have to round due to floating point errors
        rebirthProfitMultiplier = Mathf.Round(newRebirthMultiplier * 100f) / 100f;
    }

    public float GetRebirthProfitMultiplier() {
        return rebirthProfitMultiplier;
    }

    public float GetTotalProfitMultiplier() {
        // Have to round due to floating point errors
        float multiplier = profitMultiplier + levelProfitMultiplier + rebirthProfitMultiplier;

        return Mathf.Round(multiplier * 100f) / 100f;
    }

    public void PlayerRebirth() {

        if (resetMineCoroutine != null) {
            StopCoroutine(resetMineCoroutine);
        }
        if (increaseBatteryCoroutine != null) {
            StopCoroutine(increaseBatteryCoroutine);
        }
        resetMineCoroutine = StartCoroutine(ResetMine());
    }
}