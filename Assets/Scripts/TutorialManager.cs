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
    public GameObject proceedArrow;
    public GameObject proceedTutorialInstruction;

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

    private Coroutine arrowAnimation;
    private Coroutine typingMesssage;

    private IEnumerator DisplayTutorial()
    {
        // Wait for all items to be loaded
        yield return new WaitUntil(() => LoadingScreen.Instance.loadedItems >= LoadingScreen.Instance.totalItems);

        // If player reopens the game after the step where they follow the drone, make them follow the drone again
        if (tutorialScreenIndex >= 5)
        {
            MakePlayerFollowDrone();
        }

        while (tutorialScreenIndex <= 12)
        {
            Debug.Log(tutorialScreenIndex);

            AnalyticsDelegator.Instance.TutorialStep(tutorialScreenIndex);

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
            // Save for first drone upgrade
            else if (tutorialScreenIndex == 4)
            {
                MakePlayerFollowDrone();

                typingMesssage = StartCoroutine(TypeOutMessage("SAVE UP FOR THE FIRST UPGRADE!"));

                // Wait for player to accumulate enough cash for first upgrade
                yield return new WaitUntil(() => PlayerState.Instance.GetUserCash() >= 5_000);

                if (typingMesssage != null)
                {
                    StopCoroutine(typingMesssage);
                    instructionText.transform.parent.gameObject.SetActive(false);
                }
            }
            // Point to garage
            else if (tutorialScreenIndex == 5)
            {
                PointToGarage();

                // Wait for panel to open, or player to not have enough cash saved
                yield return new WaitUntil(() => droneUpgradeBayPanel.activeSelf || PlayerState.Instance.GetUserCash() < 5_000);

                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                }

                pointToGarageArrow.SetActive(false);

                // if not enough cash saved, go back
                if (PlayerState.Instance.GetUserCash() < 5_000)
                {
                    tutorialScreenIndex = 4;
                    continue;
                }
            }
            // Buy an upgrade
            else if (tutorialScreenIndex == 6)
            {
                VehicleUpgradeBayManager.Instance.FlashHeatUpgradeButton();

                // Wait until purchase, or panel closes
                yield return new WaitUntil(() => VehicleUpgradeBayManager.Instance.BoughtOneOtherUpgrade() || !droneUpgradeBayPanel.activeSelf);

                VehicleUpgradeBayManager.Instance.flashButton = false;

                // Flash for flash to stop
                yield return null;
                yield return null;

                // If they closed the panel, drop back
                if (!droneUpgradeBayPanel.activeSelf)
                {
                    tutorialScreenIndex = 5;
                    continue;
                }
            }
            // Close garage
            else if (tutorialScreenIndex == 7)
            {
                VehicleUpgradeBayManager.Instance.FlashCloseButton();

                // Wait for garage to close
                yield return new WaitUntil(() => !droneUpgradeBayPanel.activeSelf);

                VehicleUpgradeBayManager.Instance.flashButton = false;

                // Flash for flash to stop
                yield return null;
                yield return null;
            }
            // Save for first ore upgrade
            else if (tutorialScreenIndex == 8)
            {
                // Wait for player to accumulate enough cash for first upgrade
                yield return new WaitUntil(() => (double)PlayerState.Instance.GetUserCash() >= RefineryUpgradePad.Instance.GetActualMaterialPrice(0));
            }
            // Point to refinery upgrades
            else if (tutorialScreenIndex == 9)
            {
                typingMesssage = StartCoroutine(TypeOutMessage("LET'S IMPROVE THE REFINERY TO MAKE MORE MONEY!"));

                PointToRefinery();

                // Wait for panel to open, or player to not have enough cash saved
                yield return new WaitUntil(() => refineryUpgradeBayPanel.activeSelf || (double)PlayerState.Instance.GetUserCash() < RefineryUpgradePad.Instance.GetActualMaterialPrice(0));

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

                // if not enough cash saved, go back
                if ((double)PlayerState.Instance.GetUserCash() < RefineryUpgradePad.Instance.GetActualMaterialPrice(0))
                {
                    tutorialScreenIndex = 8;
                    continue;
                }
            }
            // Buy an upgrade
            else if (tutorialScreenIndex == 10)
            {
                // If player re opens the game after quitting at step 10, it throws an error here
                try
                {
                    RefineryUpgradePad.Instance.FlashOreUpgradeButton();
                }
                catch
                {
                }

                // Wait until purchase, or panel closes
                yield return new WaitUntil(() => RefineryUpgradePad.Instance.BoughtOneUpgrade() || !refineryUpgradeBayPanel.activeSelf);

                RefineryUpgradePad.Instance.flashButton = false;

                // Flash for flash to stop
                yield return null;
                yield return null;

                // If they closed the panel, drop back
                if (!refineryUpgradeBayPanel.activeSelf)
                {
                    tutorialScreenIndex = 9;
                    continue;
                }
            }
            // Close refinery
            else if (tutorialScreenIndex == 11)
            {
                RefineryUpgradePad.Instance.FlashCloseButton();

                // Wait for garage to close
                yield return new WaitUntil(() => !refineryUpgradeBayPanel.activeSelf);

                RefineryUpgradePad.Instance.flashButton = false;

                // Flash for flash to stop
                yield return null;
                yield return null;
            }
            // Let player explore by themself and wait for them to buy some more upgrades
            else if (tutorialScreenIndex == 12)
            {

                yield return new WaitUntil(() => RefineryUpgradePad.Instance.BoughtThreeUpgrades());
            }
            // Point to refinery upgrades
            else if (tutorialScreenIndex == 13)
            {
                PointToRefinery();

                // Wait for panel to open
                yield return new WaitUntil(() => refineryUpgradeBayPanel.activeSelf);

                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                }

                pointToGarageArrow.SetActive(false);
            }
            // Point to proceed panel
            else if (tutorialScreenIndex == 14)
            {
                RefineryUpgradePad.Instance.FlashProceedPanelButton();
                arrowAnimation = StartCoroutine(AnimateArrow(proceedArrow, 90, 1));

                // Wait for panel to open, or they closed the whole thing
                yield return new WaitUntil(() => refineryProceedPanel.activeSelf || !refineryUpgradeBayPanel.activeSelf);

                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                }

                proceedArrow.SetActive(false);
                RefineryUpgradePad.Instance.flashButton = false;

                // Wait for flashing to stop
                yield return null;
                yield return null;

                // if they closed the whole thing, go back
                if (!refineryUpgradeBayPanel.activeSelf)
                {
                    tutorialScreenIndex = 12;
                    continue;
                }
            }
            // Close refinery upgrades
            else if (tutorialScreenIndex == 15)
            {

                // Wait for message to stay
                yield return StartCoroutine(FlashMessage(proceedTutorialInstruction, 3, 0.3f));
            }

            tutorialScreenIndex++;
        }

        // Make sure everything is back to normal
        proceedTutorialInstruction.SetActive(false);

        // Sync values
        GameObject.Find("Settings Delegator").GetComponent<SettingsDelegator>().UpdateBools();

        finishedTutorial = true;

        try
        {
            //PlayerState.Instance.RewardPlayerWithGems(10000, "YOU FINISHED THE TUTORIAL!");

            AnalyticsDelegator.Instance.FinishTutorial();
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }

        sessionDelegator.UnlockTeam();
    }

    private void MakePlayerFollowDrone() {
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

        arrowRT.offsetMin = new Vector2(185f, arrowRT.offsetMin.y);
        arrowRT.offsetMax = new Vector2(-1328f, arrowRT.offsetMax.y);

        arrowRT.rotation = Quaternion.Euler(0, 0, 180);

        arrowAnimation = StartCoroutine(AnimateArrow(pointToGarageArrow, 90, 1));
    }

    private void PointToRefinery()
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

    private IEnumerator TeachAboutTargetDepth()
    {
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
        PointToRefinery();

        // Wait for panel open
        yield return new WaitUntil(() => refineryUpgradeBayPanel.activeSelf);

        if (arrowAnimation != null)
        {
            StopCoroutine(arrowAnimation);
        }

        pointToGarageArrow.SetActive(false);

        RefineryUpgradePad.Instance.FlashProceedPanelButton();
        arrowAnimation = StartCoroutine(AnimateArrow(proceedArrow, 90, 1));

        // Wait for panel to open, or they closed the whole thing
        yield return new WaitUntil(() => refineryProceedPanel.activeSelf);
        if (arrowAnimation != null)
        {
            StopCoroutine(arrowAnimation);
        }

        proceedArrow.SetActive(false);
        RefineryUpgradePad.Instance.flashButton = false;

        // Wait for message to stay
        yield return StartCoroutine(FlashMessage(proceedTutorialInstruction, 3, 0.3f));

        // Wait for refiner upgrade panel to close, or player clicks ok
        yield return new WaitUntil(() => !proceedTutorialInstruction.activeSelf);

        // Go back to normal
        proceedTutorialInstruction.SetActive(false);
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

    public void LoadData(GameData data) {

        TutorialUIParent.SetActive(true);

        this.finishedTutorial = data.finishedTutorial;
        this.tutorialScreenIndex = data.tutorialScreenIndex;

        // Hide supply crate button until done tutorial, and tell player how to use the camera
        if (!this.finishedTutorial)
        {
            cameraInstruction.SetActive(true);
        }

        // Hide supply crate, ad button and camera controls until second level
        if (data.mineCount < 2)
        {
            supplyCrateButton.SetActive(false);
            adButton.SetActive(false);
            cameraControls.SetActive(false);
        }

        // Hide daily challenge and target depth button until third level
        if (data.mineCount < 3)
        {
            dailyChallengeButton.SetActive(false);
            dailyChallengePlaceholder.SetActive(true);

            targetDepthButton.SetActive(false);
            targetDepthPlaceholder.SetActive(true);
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

            // Keep it at 2 for now, until we know for sure the player knows about target depth
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