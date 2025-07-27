using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour, IDataPersistence
{
    private static TutorialManager _instance;
    public static TutorialManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<TutorialManager>();
            }
            return _instance;
        }
    }

    [Header("Scripts")]
    public RefineryController refineryController;
    public SupplyCrateDelegator supplyCrateDelegator;
    public SessionDelegator sessionDelegator;

    [Header("Drone Upgrade Bay")]
    public GameObject pointToGarageArrow;
    public GameObject droneUpgradeBayPanel;

    [Header("Refinery Upgrade Bay")]
    public GameObject refineryUpgradeBayPanel;
    public GameObject refineryProceedPanel;

    [Header("Other")]
    public GameObject TutorialUIParent;
    public TextMeshProUGUI instructionText;
    public bool finishedTutorial;
    public int tutorialScreenIndex = 0; // Tracks the current tutorial screen
    private int highestLevelReached;
    public GameObject cameraInstruction;
    public GameObject targetDepthPanel;
    public Button cameraModeSwitch;

    [Header("Notice Icons")]
    public GameObject leaderboardNoticeIcon;
    public GameObject premiumShopNoticeIcon;

    [Header("UI Buttons")]
    public GameObject dailyChallengeButton;
    public GameObject dailyChallengePlaceholder;
    public GameObject supplyCrateButton;
    public GameObject leaderboardButton;
    public GameObject targetDepthButton;
    public GameObject targetDepthPlaceholder;
    public GameObject adButton;
    public GameObject cameraControls;
    public GameObject proceedButton;

    private Coroutine arrowAnimation;
    private Coroutine typingMesssage;

    private IEnumerator DisplayTutorial()
    {
        // Wait for all items to be loaded
        yield return new WaitUntil(() => LoadingScreen.Instance.loadedItems >= LoadingScreen.Instance.totalItems);
        yield return new WaitForSeconds(0.5f);

        if (RefineryUpgradePad.Instance.BoughtTenUpgrades() && tutorialScreenIndex < 6)
        {
            tutorialScreenIndex = 6;
        }

        if (RefineryUpgradePad.Instance.Bought25Upgrades() && tutorialScreenIndex < 8)
        {
            tutorialScreenIndex = 8;
        }

        while (tutorialScreenIndex <= 12)
        {
            Debug.Log(tutorialScreenIndex);

            AnalyticsDelegator.Instance.TutorialStep(tutorialScreenIndex);

            // Proceed panel is unlocked after this step
            if (tutorialScreenIndex >= 7)
            {
                proceedButton.SetActive(true);
            }
            
            // Follow drone once available
            if (VehicleUpgradeBayManager.Instance.BoughtOneDroneUpgrade())
            {
                MakePlayerFollowDrone();
            }

            // Load mine
            if (tutorialScreenIndex == 0)
            {
                // Wait for mine to load
                yield return new WaitUntil(() => refineryController.mineRenderer.mineInitialization != 0);
            }
            // Point to garage
            else if (tutorialScreenIndex == 1)
            {
                typingMesssage = StartCoroutine(TypeOutMessage("LET'S BUY OUR FIRST DRONE!"));

                PointToGarage();

                // Wait for panel to open
                yield return new WaitUntil(() => droneUpgradeBayPanel.activeSelf);

                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                }

                if (typingMesssage != null)
                {
                    StopCoroutine(typingMesssage);
                    instructionText.transform.parent.gameObject.SetActive(false);
                }

                pointToGarageArrow.SetActive(false);
            }
            // Buy a drone
            else if (tutorialScreenIndex == 2)
            {
                VehicleUpgradeBayManager.Instance.FlashDroneUpgradeButton();

                // Wait until purchase, or panel closes
                yield return new WaitUntil(() => VehicleUpgradeBayManager.Instance.BoughtOneDroneUpgrade() || !droneUpgradeBayPanel.activeSelf);

                VehicleUpgradeBayManager.Instance.flashButton = false;

                // Flash for flash to stop
                yield return null;
                yield return null;

                // If they closed the panel, drop back
                if (!droneUpgradeBayPanel.activeSelf)
                {
                    tutorialScreenIndex = 1;
                    continue;
                }
            }
            // Close garage
            else if (tutorialScreenIndex == 3)
            {
                VehicleUpgradeBayManager.Instance.FlashCloseButton();

                // Wait for garage to close
                yield return new WaitUntil(() => !droneUpgradeBayPanel.activeSelf);

                VehicleUpgradeBayManager.Instance.flashButton = false;

                // Flash for flash to stop
                yield return null;
                yield return null;
            }
            // Let player explore by themself for a bit. They buy some upgrades on their own, and then maybe run out of cash
            else if (tutorialScreenIndex == 4)
            {
                yield return new WaitUntil(() => RefineryUpgradePad.Instance.BoughtTenUpgrades() || (RefineryUpgradePad.Instance.BoughtThreeUpgrades() && ((double)PlayerState.Instance.GetUserCash() < RefineryUpgradePad.Instance.GetMaterialUpgradePrice(0))));
            }
            // Close panel
            else if (tutorialScreenIndex == 5)
            {
                //refineryInstruction.SetActive(true);
                yield return new WaitUntil(() => !refineryUpgradeBayPanel.activeSelf);
                //refineryInstruction.SetActive(false);
            }
            // ENSURE that they bought at least 10
            else if (tutorialScreenIndex == 6)
            {
                yield return new WaitUntil(() => RefineryUpgradePad.Instance.BoughtTenUpgrades());
                //refineryInstruction.SetActive(true);
            }
            // Point to refinery upgrades
            else if (tutorialScreenIndex == 7)
            {
                PointToProceed();

                // Wait for panel to open
                yield return new WaitUntil(() => refineryProceedPanel.activeSelf);
                //refineryInstruction.SetActive(false);

                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                }

                pointToGarageArrow.SetActive(false);
            }
            else if (tutorialScreenIndex == 8)
            {
                // Tell player to upgrade their ore
                //upgradeRefineryInstruction.SetActive(true);

                // Wait to tell the player to buy the profit ugrade
                yield return new WaitUntil(() => PlayerState.Instance.GetUserCash() >= VehicleUpgradeBayManager.Instance.firstProfitUpgradePrice || VehicleUpgradeBayManager.Instance.BoughtProfitUpgrade());

                // If already purchased then skip ahead
                if (VehicleUpgradeBayManager.Instance.BoughtProfitUpgrade())
                {
                    tutorialScreenIndex = 9;
                    continue;
                }

                //upgradeRefineryInstruction.SetActive(false);

                //StartCoroutine(FlashMessage(refineryInstruction, 3, 0.5f));
            }
            else if (tutorialScreenIndex == 9)
            {
                // If already purchased then skip ahead
                if (VehicleUpgradeBayManager.Instance.BoughtProfitUpgrade())
                {
                    tutorialScreenIndex = 10;
                    continue;
                }

                PointToGarage();

                // Wait for panel to open
                yield return new WaitUntil(() => droneUpgradeBayPanel.activeSelf);

                pointToGarageArrow.SetActive(false);

                // If they spent some, give them the rest that they need
                if (PlayerState.Instance.GetUserCash() < VehicleUpgradeBayManager.Instance.firstProfitUpgradePrice)
                {
                    PlayerState.Instance.AddCash(VehicleUpgradeBayManager.Instance.firstProfitUpgradePrice - (double)PlayerState.Instance.GetUserCash());
                }

                VehicleUpgradeBayManager.Instance.FlashProfitUpgradeButton();
            }
            else if (tutorialScreenIndex == 10)
            {
                // Wait for player to buy the upgrade
                yield return new WaitUntil(() => VehicleUpgradeBayManager.Instance.BoughtProfitUpgrade() || !droneUpgradeBayPanel.activeSelf);

                VehicleUpgradeBayManager.Instance.flashButton = false;

                // Player didn't buy upgrade
                if (!VehicleUpgradeBayManager.Instance.BoughtProfitUpgrade())
                {
                    tutorialScreenIndex = 9;
                    continue;
                }

                // Back to old instruction
                //refineryInstruction.SetActive(false);
                //upgradeRefineryInstruction.SetActive(true);

                VehicleUpgradeBayManager.Instance.FlashCloseButton();

                // Close panel
                yield return new WaitUntil(() => !droneUpgradeBayPanel.activeSelf);

                VehicleUpgradeBayManager.Instance.flashButton = false;
            }
            else if (tutorialScreenIndex == 11)
            {
                // Wait for player to buy max upgrades needed
                yield return new WaitUntil(() => RefineryUpgradePad.Instance.Bought25Upgrades());
            }
            else if (tutorialScreenIndex == 12)
            {
                PointToProceed();

                // Wait for panel to open
                yield return new WaitUntil(() => refineryProceedPanel.activeSelf);

                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                }

                pointToGarageArrow.SetActive(false);
            }

            tutorialScreenIndex++;
        }

        // Sync values
        GameObject.Find("Settings Delegator").GetComponent<SettingsDelegator>().UpdateBools();

        finishedTutorial = true;

        try
        {
            //PlayerState.Instance.RewardPlayerWithGems(10000, "YOU FINISHED THE TUTORIAL!");

            DoneTutorial();
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }

        sessionDelegator.UnlockTeam();
    }

    public void DoneTutorial()
    {
        // Only send event once
        if (!finishedTutorial)
        {
            try
            {
                AnalyticsDelegator.Instance.FinishTutorial();
            }
            catch
            {
            }
        }

        this.finishedTutorial = true;
    }

    public void MakePlayerFollowDrone() {
        // If already following a drone, do nothing
        if (GameCameraController.Instance.droneToFollow != null)
        {
            return;
        }

        // Make player follow their drone
        cameraModeSwitch.onClick.Invoke();
        // Don't need this since the camera is following the drone
        cameraInstruction.SetActive(false);
    }

    private IEnumerator AnimateArrow(GameObject arrow, float amplitude, int axis) {
        // axis 0 = x, axis 1 = y

        arrow.SetActive(true);

        RectTransform rectTransform = arrow.GetComponent<RectTransform>();
        // Save the original position for reference
        Vector2 originalPos = rectTransform.anchoredPosition;

        const float speed = 3f;      // Controls the speed of the oscillation

        // Animate horizontally
        if (axis == 0)
        {
            while (true)
            {
                // Calculate the new x offset using Mathf.Sin
                float offsetX = Mathf.Sin(Time.time * speed) * amplitude;

                // Update the anchored position while preserving the y-coordinate
                rectTransform.anchoredPosition = new Vector2(originalPos.x + offsetX, originalPos.y);

                // Wait until the next frame
                yield return null;
            }
        }
        // Animate vertically
        else
        {
            while (true)
            {
                // Calculate the new y offset using Mathf.Sin
                float offsetY = Mathf.Sin(Time.time * speed) * amplitude;

                // Update the anchored position while preserving the x-coordinate
                rectTransform.anchoredPosition = new Vector2(originalPos.x, originalPos.y + offsetY);

                // Wait until the next frame
                yield return null;
            }
        }
    }

    private IEnumerator FlashMessage(GameObject msg, int flashes, float interval) {
        for (int i = 0; i < flashes; i++) {
            msg.SetActive(false);
            yield return new WaitForSeconds(interval);
            msg.SetActive(true);
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator TypeOutMessage(string messageKey)
    {
        string messageToType = GetLocalizedValue(messageKey);

        instructionText.transform.parent.gameObject.SetActive(true);

        // Clear previous text
        instructionText.text = "";

        const float delay = 0.05f; // 20 characters per second

        string output = "";

        foreach (char letter in messageToType)
        {
            output += letter;
            instructionText.text = output + "|";

            yield return new WaitForSeconds(delay);
        }

        instructionText.text = output;
    }

    private void PointToGarage()
    {
        RectTransform arrowRT = pointToGarageArrow.GetComponent<RectTransform>();

        // Use bottom-stretch positioning
        arrowRT.anchorMin = new Vector2(0, 0); // left-bottom
        arrowRT.anchorMax = new Vector2(1, 0); // right-bottom
        arrowRT.pivot = new Vector2(0.5f, 0); // top center

        Vector2 p = arrowRT.anchoredPosition;

        p.y = 733f;
        arrowRT.anchoredPosition = p;

        arrowRT.offsetMin = new Vector2(374f, arrowRT.offsetMin.y);
        arrowRT.offsetMax = new Vector2(-1356f, arrowRT.offsetMax.y);

        arrowRT.rotation = Quaternion.Euler(0, 0, 180);

        arrowAnimation = StartCoroutine(AnimateArrow(pointToGarageArrow, 90, 1));
    }

    private void PointToProceed()
    {
        RectTransform arrowRT = pointToGarageArrow.GetComponent<RectTransform>();

        // Use top-stretch positioning
        arrowRT.anchorMin = new Vector2(0, 1); // left-top
        arrowRT.anchorMax = new Vector2(1, 1); // right-top
        arrowRT.pivot = new Vector2(0.5f, 1);   // top center

        Vector2 p = arrowRT.anchoredPosition;
        p.y = -1443;
        arrowRT.anchoredPosition = p;

        arrowRT.offsetMin = new Vector2(1580f, arrowRT.offsetMin.y);
        arrowRT.offsetMax = new Vector2(67f, arrowRT.offsetMax.y);

        arrowRT.rotation = Quaternion.Euler(0, 0, 0);

        arrowAnimation = StartCoroutine(AnimateArrow(pointToGarageArrow, 90, 1));
    }

    private IEnumerator PointToDrill(float requiredHoldTime)
    {
        float extraRequiredTime = requiredHoldTime * 3.5f;
        yield return new WaitUntil(() => NPCManager.Instance.pointToDrillArrow != null);

        GameObject arrow = NPCManager.Instance.pointToDrillArrow;
        StartCoroutine(AnimateArrow(arrow, 1f, 1));

        // It gets enabled in animate arrow, so immediately disable it
        yield return null;
        arrow.SetActive(false);

        float affordStartTime = -1f;

        // Whether or not the player listened to the arrow or not at least one
        bool listenedOnce = false;

        while (true)
        {

            // If they can afford something and there's no other arrow showing or done the tutorial
            // Show the arrow
            if (RefineryUpgradePad.Instance.CanAffordAnUpgrade()
            && (finishedTutorial || (tutorialScreenIndex != 1
            && tutorialScreenIndex != 7
            && tutorialScreenIndex != 9
            && tutorialScreenIndex != 12)))
            {
                // first time it becomes affordable, stamp the time
                if (affordStartTime < 0f)
                    affordStartTime = Time.realtimeSinceStartup;

                // Don't need arrow if panel is open
                else if (refineryUpgradeBayPanel.activeSelf || refineryProceedPanel.activeSelf)
                {
                    affordStartTime = Time.realtimeSinceStartup;
                    arrow.SetActive(false);
                }

                if (!listenedOnce)
                {
                    // if it's been affordable for >= requiredHoldTime, show the arrow
                    if (Time.realtimeSinceStartup - affordStartTime >= requiredHoldTime)
                        arrow.SetActive(true);
                }
                else
                {
                    // Use this timer if they already know
                    if (Time.realtimeSinceStartup - affordStartTime >= extraRequiredTime)
                        arrow.SetActive(true);
                }

            }
            // Otherwise hide it
            else
            {
                affordStartTime = -1f;
                arrow.SetActive(false);
            }

            // After tutorial level
            if (RefineryUpgradePad.Instance.BoughtThreeUpgrades())
            {
                listenedOnce = true;
            }
            // During tutorial level
            else if (tutorialScreenIndex > 6)
            {
                listenedOnce = true;
            }

            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    private IEnumerator TeachAboutTargetDepth()
    {
        yield return new WaitUntil(() => LoadingScreen.Instance.loadedItems >= LoadingScreen.Instance.totalItems);

        if (!VehicleUpgradeBayManager.Instance.BoughtOneDroneUpgrade())
        {
            PointToGarage();

            // Wait for panel to open
            yield return new WaitUntil(() => droneUpgradeBayPanel.activeSelf);

            if (arrowAnimation != null)
            {
                StopCoroutine(arrowAnimation);
            }

            pointToGarageArrow.SetActive(false);

            // Wait for player to buy a drone
            yield return new WaitUntil(() => VehicleUpgradeBayManager.Instance.BoughtOneDroneUpgrade());
        }

        // Wait for panel to close
        yield return new WaitUntil(() => !droneUpgradeBayPanel.activeSelf);

        // Tell them to open the target depth panel up
        RectTransform arrowRT = pointToGarageArrow.GetComponent<RectTransform>();

        // Use top-stretch positioning
        arrowRT.anchorMin = new Vector2(0, 1); // left-top
        arrowRT.anchorMax = new Vector2(1, 1); // right-top
        arrowRT.pivot = new Vector2(0.5f, 1);   // top center

        Vector2 p = arrowRT.anchoredPosition;
        p.y = -873;
        arrowRT.anchoredPosition = p;

        arrowRT.offsetMin = new Vector2(1580f, arrowRT.offsetMin.y);
        arrowRT.offsetMax = new Vector2(67f, arrowRT.offsetMax.y);

        arrowRT.rotation = Quaternion.Euler(0, 0, 0);

        arrowAnimation = StartCoroutine(AnimateArrow(pointToGarageArrow, 90, 1));

        yield return new WaitUntil(() => targetDepthPanel.activeSelf);

        if (arrowAnimation != null)
        {
            StopCoroutine(arrowAnimation);
        }

        pointToGarageArrow.SetActive(false);

        // Now the player knows about target depth
        this.highestLevelReached = 3;
    }

    private IEnumerator RemindAboutProceedRequirement()
    {
        yield return new WaitUntil(() => LoadingScreen.Instance.loadedItems >= LoadingScreen.Instance.totalItems);

        // Point to refinery upgrades
        PointToProceed();

        // Wait for panel open
        yield return new WaitUntil(() => refineryProceedPanel.activeSelf);

        if (arrowAnimation != null)
        {
            StopCoroutine(arrowAnimation);
        }
        
        pointToGarageArrow.SetActive(false);

        // And then we remind them to open the garage if they haven't bought anything yet 
        if (!VehicleUpgradeBayManager.Instance.BoughtOneDroneUpgrade() && !VehicleUpgradeBayManager.Instance.BoughtOneOtherUpgrade())
        {
            PointToGarage();

            yield return new WaitUntil(() => droneUpgradeBayPanel.activeSelf);

            if (arrowAnimation != null)
            {
                StopCoroutine(arrowAnimation);
            }

            pointToGarageArrow.SetActive(false);
        }
    }

    private string GetLocalizedValue(string key, params object[] args)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UI Tables");

        StringTableEntry entry = table.GetEntry(key); ;

        // If no translation, just return the key
        if (entry == null)
        {
            return string.Format(key, args);
        }

        // Use string.Format to replace placeholders with arguments
        return string.Format(entry.LocalizedValue, args);
    }

    public void TellPlayerToMove() {
        Transform movementJoystick = JoystickMovement.Instance.transform;
        
        for (int i = 0; i != movementJoystick.childCount; i++)
        {
            movementJoystick.GetChild(i).transform.localPosition = new(300, -540);
            movementJoystick.GetChild(i).gameObject.SetActive(true);
        }
    }

    public void LoadData(GameData data)
    {
        // Set up A/B
        string cohort = PlayerPrefs.GetString("Cohort", "No Cohort");

        if (cohort == "No Cohort")
        {
            System.Random cohortRNG = new();
            double rand = cohortRNG.NextDouble();

            // 35/15/35/15 split
            if (rand < 0.35)
            {
                cohort = "A";
            }
            else if (rand < 0.5)
            {
                cohort = "B";
            }
            else if (rand < 0.85)
            {
                cohort = "C";
            }
            else
            {
                cohort = "D";
            }

            PlayerPrefs.SetString("Cohort", cohort);
        }

        TutorialUIParent.SetActive(true);

        this.finishedTutorial = data.finishedTutorial;
        this.tutorialScreenIndex = data.tutorialScreenIndex;

        // Hide supply crate button until done tutorial, and tell player how to use the camera
        if (!this.finishedTutorial)
        {
            cameraInstruction.SetActive(true);
        }

        // Hide supply crate, ad button and camera controls until second level. Hide proceed button until that step is ready.
        // Point to the drill if needed
        if (data.mineCount < 2)
        {
            supplyCrateButton.SetActive(false);
            adButton.SetActive(false);
            cameraControls.SetActive(false);

            if (tutorialScreenIndex < 7)
            {
                proceedButton.SetActive(false);
            }

            StartCoroutine(PointToDrill(3f));
        }

        // Hide daily challenge and target depth button until third level
        // Point to the drill if needed
        if (data.mineCount < 3)
        {
            dailyChallengeButton.SetActive(false);
            dailyChallengePlaceholder.SetActive(true);

            targetDepthButton.SetActive(false);
            targetDepthPlaceholder.SetActive(true);

            // Make sure it's not enabled on the first level, otherwise there are two coroutines going at once
            if (data.mineCount == 2)
            {
                StartCoroutine(PointToDrill(6f));
            }
        }

        // Hide leaderboard button until fourth level
        if (data.mineCount < 4)
        {
            leaderboardButton.SetActive(false);
        }

        // If its the first time the player is reaching this left, draw attention to the leaderboard
        if (data.highestLevelReached < 3)
        {
            leaderboardNoticeIcon.SetActive(true);
        }

        this.highestLevelReached = data.mineCount;

        // Remind player how to reach the next level
        if (data.highestLevelReached == 1 && data.mineCount >= 2)
        {
            StartCoroutine(RemindAboutProceedRequirement());
        }
        // If first time reaching level 3, tell them how target depth works.
        else if (data.highestLevelReached == 2 && data.mineCount >= 3)
        {
            StartCoroutine(TeachAboutTargetDepth());

            // Keep THIS.highestLevelReached at 2 for now, until we know for sure the player knows about target depth
            this.highestLevelReached = 2;
        }

        try
        {
            if (this.finishedTutorial)
            {
                sessionDelegator.UnlockTeam();
                return;
            }
        }
        catch
        {
            return;
        }

        if (tutorialScreenIndex == 0)
        {
            AnalyticsDelegator.Instance.StartTutorial();
        }

        StartCoroutine(DisplayTutorial());
    }

    public void SaveData(ref GameData data)
    {
        data.finishedTutorial = this.finishedTutorial;
        data.tutorialScreenIndex = this.tutorialScreenIndex;
        data.highestLevelReached = this.highestLevelReached;
    }

}