using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour, IDataPersistence
{

    [Header("Scripts")]
    public PlayerState playerState;
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
    public Button refineryOresButton;

    [Header("Other")]
    public GameObject TutorialUIParent;
    public TextMeshProUGUI instructionText;
    public bool finishedTutorial;
    public int tutorialScreenIndex = 0; // Tracks the current tutorial screen
    private int highestLevelReached;
    public GameObject cameraInstruction;
    public GameObject loadingScreen;
    public GameObject targetDepthPanel;

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

    private Coroutine arrowAnimation;
    private Coroutine typingMesssage;

    private IEnumerator DisplayTutorial()
    {
        // Wait for the loading screen to be deactivated
        yield return new WaitUntil(() => !loadingScreen.activeSelf);

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
                typingMesssage = StartCoroutine(TypeOutMessage("SAVE UP FOR THE FIRST UPGRADE!"));

                // Wait for player to accumulate enough cash for first upgrade
                yield return new WaitUntil(() => playerState.GetUserCash() >= 5_000);

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
                yield return new WaitUntil(() => droneUpgradeBayPanel.activeSelf || playerState.GetUserCash() < 5_000);

                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                }

                pointToGarageArrow.SetActive(false);

                // if not enough cash saved, go back
                if (playerState.GetUserCash() < 5_000)
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
                yield return new WaitUntil(() => (double)playerState.GetUserCash() >= RefineryUpgradePad.Instance.GetActualMaterialPrice(0));
            }
            // Point to refinery upgrades
            else if (tutorialScreenIndex == 9)
            {
                typingMesssage = StartCoroutine(TypeOutMessage("LET'S IMPROVE THE REFINERY TO MAKE MORE MONEY!"));

                RectTransform arrowRT = pointToGarageArrow.GetComponent<RectTransform>();

                Vector2 p = arrowRT.anchoredPosition;
                p.y = 1840f;
                arrowRT.anchoredPosition = p;

                arrowRT.offsetMin = new Vector2(1580f, arrowRT.offsetMin.y);
                arrowRT.offsetMax = new Vector2(67f, arrowRT.offsetMax.y);

                arrowRT.rotation = Quaternion.Euler(0, 0, 0);

                arrowAnimation = StartCoroutine(AnimateArrow(pointToGarageArrow, 90, 1));

                // Wait for panel to open, or player to not have enough cash saved
                yield return new WaitUntil(() => refineryUpgradeBayPanel.activeSelf || (double)playerState.GetUserCash() < RefineryUpgradePad.Instance.GetActualMaterialPrice(0));

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
                if ((double)playerState.GetUserCash() < RefineryUpgradePad.Instance.GetActualMaterialPrice(0))
                {
                    tutorialScreenIndex = 8;
                    continue;
                }
            }
            // Buy an upgrade
            else if (tutorialScreenIndex == 10)
            {
                RefineryUpgradePad.Instance.FlashOreUpgradeButton();

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
            // Point to proceed panel
            else if (tutorialScreenIndex == 11)
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
                    tutorialScreenIndex = 9;
                    continue;
                }
            }
            // Close refinery upgrades
            else if (tutorialScreenIndex == 12)
            {
                refineryOresButton.interactable = false;

                // Wait for message to stay
                yield return StartCoroutine(FlashMessage(proceedTutorialInstruction, 3, 0.3f));

                // Wait for refiner upgrade panel to close, or player clicks ok
                yield return new WaitUntil(() => !proceedTutorialInstruction.activeSelf || !refineryUpgradeBayPanel.activeSelf);

                RefineryUpgradePad.Instance.flashButton = false;
            }

            tutorialScreenIndex++;
        }

        // Make sure everything is back to normal
        refineryOresButton.interactable = true;
        proceedTutorialInstruction.SetActive(false);
        refineryUpgradeBayPanel.SetActive(false);
        supplyCrateButton.SetActive(true);
        UIDelegation.Instance.RevealAll();
        GameCameraController.Instance.ToggleMovement(true);

        // Sync values
        GameObject.Find("Settings Delegator").GetComponent<SettingsDelegator>().UpdateBools();

        finishedTutorial = true;

        try
        {
            playerState.RewardPlayerWithGems(10000, "YOU FINISHED THE TUTORIAL!");
            supplyCrateDelegator.ChangeCrateCount(1);

            AnalyticsDelegator.Instance.FinishTutorial();
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }

        sessionDelegator.UnlockTeam();
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

        Vector2 p = arrowRT.anchoredPosition;

        p.y = 563f;
        arrowRT.anchoredPosition = p;

        arrowRT.offsetMin = new Vector2(185f, arrowRT.offsetMin.y);
        arrowRT.offsetMax = new Vector2(-1328f, arrowRT.offsetMax.y);

        arrowRT.rotation = Quaternion.Euler(0, 0, 180);

        arrowAnimation = StartCoroutine(AnimateArrow(pointToGarageArrow, 90, 1));
    }

    private IEnumerator TeachAboutTargetDepth()
    {
        if (!VehicleUpgradeBayManager.Instance.BoughtOneDroneUpgrade())
        {
            PointToGarage();
        }

        // Wait for panel to open
        yield return new WaitUntil(() => droneUpgradeBayPanel.activeSelf);

        if (arrowAnimation != null)
        {
            StopCoroutine(arrowAnimation);
        }

        pointToGarageArrow.SetActive(false);

        // Wait for player to buy a drone
        yield return new WaitUntil(() => VehicleUpgradeBayManager.Instance.BoughtOneDroneUpgrade());

        // Wait for panel to close
        yield return new WaitUntil(() => !droneUpgradeBayPanel.activeSelf);

        // Tell them to open the target depth panel up
        RectTransform arrowRT = pointToGarageArrow.GetComponent<RectTransform>();

        Vector2 p = arrowRT.anchoredPosition;
        p.y = 2410f;
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
        this.highestLevelReached = 2;
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
            supplyCrateButton.SetActive(false);
            cameraInstruction.SetActive(true);
        }

        // Hide daily challenge and target depth button until second level. And the ad button too.
        if (data.mineCount < 2)
        {
            dailyChallengeButton.SetActive(false);
            dailyChallengePlaceholder.SetActive(true);

            targetDepthButton.SetActive(false);
            targetDepthPlaceholder.SetActive(true);

            adButton.SetActive(false);
        }

        // Hide leaderboard button until third level
        if (data.mineCount < 3)
        {
            leaderboardButton.SetActive(false);
        }

        // If its the first time the player is reaching this left, draw attention to the leaderboard
        if (data.highestLevelReached < 3)
        {
            leaderboardNoticeIcon.SetActive(true);
        }
        
        this.highestLevelReached = data.mineCount;

        // If first time reaching level 2, tell them how target depth works.
        if (data.highestLevelReached == 1)
        {
            StartCoroutine(TeachAboutTargetDepth());

            // Keep it at 1 for now, until we know for sure the player knows about target depth
            this.highestLevelReached = 1;
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