using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour, IDataPersistence
{

    public PlayerState playerState;
    public PlayerMovement playerMovement;
    public PlayerVehicleDelegation playerVehicleDelegation;
    public RefineryController refineryController;
    public RefineryUpgradePad refineryUpgradePad;
    public SupplyCrateDelegator supplyCrateDelegator;
    public SessionDelegator sessionDelegator;
    public UpgradesDelegator upgradesDelegator;
    public VehicleUpgradeBayManager vehicleUpgradeBayManager;

    public Slider playerHeatSlider;

    public GameObject ResetMine;
    public GameObject TutorialUIParent;
    public GameObject powerIndicator;
    public GameObject enterMineArrow;
    public RectTransform movementJoystick;
    public GameObject playerMessage;
    public GameObject vehicleUpgradeBayPanel;
    public GameObject refineryUpgradeBayPanel;
    public GameObject refineryProceedPanel;
    public GameObject overHeatTip;
    public GameObject proceedArrow;
    public GameObject proceedTutorialInstruction;
    public Button refineryOresButton;

    public bool finishedTutorial;
    public int tutorialScreenIndex = 0; // Tracks the current tutorial screen
    public GameObject loadingScreen;

    public GameObject leaderboardNoticeIcon;
    public GameObject premiumShopNoticeIcon;

    private Coroutine arrowAnimation;

    private IEnumerator DisplayTutorial()
    {
        ResetMine.SetActive(false);

        // Wait for the loading screen to be deactivated
        yield return new WaitUntil(() => !loadingScreen.activeSelf);

        while (tutorialScreenIndex <= 11)
        {
            Debug.Log(tutorialScreenIndex);
            
            AnalyticsDelegator.Instance.TutorialStep(tutorialScreenIndex);

            // Load mine
            if (tutorialScreenIndex == 0)
            {
                TellPlayerToMove();

                yield return new WaitUntil(() => refineryController.mineRenderer.mineInitialization != 0);

            }
            // Go into mine
            else if (tutorialScreenIndex == 1)
            {
                arrowAnimation = StartCoroutine(AnimateArrow(enterMineArrow, 2, 1));

                yield return new WaitUntil(() => IsInTheMine(playerMovement.transform.position.y));

                enterMineArrow.SetActive(false);
                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                }

            }
            // Use survey radar
            else if (tutorialScreenIndex == 2)
            {

                // In case player already activated power
                upgradesDelegator.cooldownTimer = 0;
                upgradesDelegator.scannedForOres = false;

                powerIndicator.SetActive(true);
                TutorialUIParent.SetActive(true);
                arrowAnimation = StartCoroutine(AnimateArrow(powerIndicator.transform.GetChild(0).gameObject, 30, 1));

                yield return new WaitUntil(() => upgradesDelegator.scannedForOres);

                TutorialUIParent.SetActive(false);
                powerIndicator.SetActive(false);
                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                }

            }
            // Mine revealed ores
            else if (tutorialScreenIndex == 3)
            {
                TellPlayerToMove();
                yield return new WaitUntil(() => playerState.materialsSold > 1);
            }
            // Mine until the time runs out
            else if (tutorialScreenIndex == 4)
            {
                refineryController.refineryTimer = 30;

                playerMessage.SetActive(true);
                // flash 3 times
                yield return StartCoroutine(FlashMessage(playerMessage, 3, 0.4f));
                // Ensure it stays active
                playerMessage.SetActive(true);

                // Warn the user about drill heat at about this point
                yield return new WaitUntil(() => refineryController.refineryTimer == 17);

                // Enable overheat tip
                TutorialUIParent.SetActive(true);
                overHeatTip.SetActive(true);
                // flash 3 times
                yield return StartCoroutine(FlashMessage(overHeatTip, 3, 0.4f));
                // Ensure it stays active
                overHeatTip.SetActive(true);

                // Wait until timer reaches 0, or starts to reset and goes above 30
                yield return new WaitUntil(() => refineryController.refineryTimer == 0 || refineryController.refineryTimer > 30);
                TutorialUIParent.SetActive(false);
                overHeatTip.SetActive(false);

                // Make sure player has at least 25k cash
                if (playerState.GetUserCash() < 25000)
                {
                    playerState.AddCash((double)(25000 - playerState.GetUserCash()));
                }
            }
            // Go to the vehicle upgrade bay
            else if (tutorialScreenIndex == 5)
            {
                playerMessage.SetActive(false);

                // Flip and show arrow
                enterMineArrow.transform.eulerAngles = new(0, 0, 90);
                enterMineArrow.transform.localPosition = new(-1083.79f, -1911.5f, 0);
                arrowAnimation = StartCoroutine(AnimateArrow(enterMineArrow, 2, 0));

                yield return new WaitUntil(() => vehicleUpgradeBayPanel.activeSelf);

                enterMineArrow.SetActive(false);
                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                }
            }
            // Upgrade heat limit
            else if (tutorialScreenIndex == 6)
            {
                playerMessage.SetActive(false);

                // Indicate upgrade button
                vehicleUpgradeBayManager.FlashUpgradeButton();

                // Wait until they buy an upgrade
                // Or if they close the panel
                yield return new WaitUntil(() => vehicleUpgradeBayManager.BoughtOneUpgrade() || !vehicleUpgradeBayPanel.activeSelf);

                vehicleUpgradeBayManager.flashButton = false;

                // Wait for flashing to stop
                yield return null;
                yield return null;
                yield return null;

                // If they closed the panel, drop the index back to 5
                if (!vehicleUpgradeBayPanel.activeSelf)
                {
                    tutorialScreenIndex = 5;
                    continue;
                }
            }
            // Close vehicle upgrade bay
            else if (tutorialScreenIndex == 7)
            {
                // Indicate close button
                vehicleUpgradeBayManager.FlashCloseButton();

                yield return new WaitUntil(() => !vehicleUpgradeBayPanel.activeSelf);

                vehicleUpgradeBayManager.flashButton = false;
            }
            // Go to refinery upgrade bay
            else if (tutorialScreenIndex == 8)
            {
                // Flip and show arrow
                enterMineArrow.transform.eulerAngles = new(0, 0, 270);
                enterMineArrow.transform.localPosition = new(-1076.21f, -1911.5f, 0);
                arrowAnimation = StartCoroutine(AnimateArrow(enterMineArrow, 2, 0));

                yield return new WaitUntil(() => refineryUpgradeBayPanel.activeSelf);

                enterMineArrow.SetActive(false);
                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                }
            }
            // Upgrade limestone ore
            else if (tutorialScreenIndex == 9)
            {
                // Wait for it to load
                yield return new WaitUntil(() => refineryUpgradePad.limestoneUpgradeImage != null || !refineryUpgradeBayPanel.activeSelf);

                // If they closed the panel, drop the index back to 8
                if (!refineryUpgradeBayPanel.activeSelf)
                {
                    tutorialScreenIndex = 8;
                    continue;
                }

                refineryUpgradePad.FlashOreUpgradeButton();

                yield return new WaitUntil(() => refineryUpgradePad.BoughtOneUpgrade() || !refineryUpgradeBayPanel.activeSelf);

                refineryUpgradePad.flashButton = false;

                // Wait for flashing to stop
                yield return null;
                yield return null;
                yield return null;

                // If they closed the panel, drop the index back to 8
                if (!refineryUpgradeBayPanel.activeSelf)
                {
                    tutorialScreenIndex = 8;
                    continue;
                }
            }
            // Go to proceed panel
            else if (tutorialScreenIndex == 10)
            {
                // Indicate proceed button
                refineryUpgradePad.FlashProceedPanelButton();
                // Animate arrow too (and ensure proper placement)
                RectTransform arrowRT = proceedArrow.GetComponent<RectTransform>();
                Vector2 p = arrowRT.anchoredPosition;
                p.y = -1030f;
                arrowRT.anchoredPosition = p;
                arrowAnimation = StartCoroutine(AnimateArrow(proceedArrow, 90, 1));

                // Wait until they buy an upgrade
                // Or if they close the panel
                yield return new WaitUntil(() => refineryProceedPanel.activeSelf || !refineryUpgradeBayPanel.activeSelf);

                refineryUpgradePad.flashButton = false;

                // Wait for flashing to stop
                yield return null;
                yield return null;
                yield return null;

                proceedArrow.SetActive(false);
                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                }

                // If they closed the panel, drop the index back to 8
                if (!refineryUpgradeBayPanel.activeSelf)
                {
                    tutorialScreenIndex = 8;
                    continue;
                }
            }
            // Close refinery upgrade bay
            else if (tutorialScreenIndex == 11)
            {
                // Can't go click on ores tab until tutorial ends
                refineryOresButton.interactable = false;

                // Show message
                yield return StartCoroutine(FlashMessage(proceedTutorialInstruction, 3, 0.3f));

                // Wait for refinery panel to close, or player clicks ok
                yield return new WaitUntil(() => !proceedTutorialInstruction.activeSelf || !refineryUpgradeBayPanel.activeSelf);

                refineryUpgradePad.flashButton = false;
            }

            tutorialScreenIndex++;
        }

        // Can now reset mine
        ResetMine.SetActive(true);
        // Make sure everything is back to normal
        refineryOresButton.interactable = true;
        proceedTutorialInstruction.SetActive(false);
        refineryUpgradeBayPanel.SetActive(false);
        supplyCrateDelegator.uIDelegation.RevealAll();

        // Sync values
        GameObject.Find("Settings Delegator").GetComponent<SettingsDelegator>().UpdateBools();
        finishedTutorial = true;

        try {
            playerState.RewardPlayerWithGems(10000, "YOU FINISHED THE TUTORIAL!");
            supplyCrateDelegator.ChangeCrateCount(1);
            
            // Reset mine
            refineryController.CallResetMineFromButton();

            AnalyticsDelegator.Instance.FinishTutorial();
        } catch (Exception ex) {  
            Debug.Log(ex.Message);
        }

        leaderboardNoticeIcon.SetActive(true);
        //premiumShopNoticeIcon.SetActive(true);
        sessionDelegator.UnlockTeam();
        Destroy(TutorialUIParent);
    }

    public void TellPlayerToMove() {
        playerMovement.stopMoving = false;

        for (int i = 0; i != movementJoystick.childCount; i++) {
            movementJoystick.GetChild(i).transform.localPosition = new(0, -540);
            movementJoystick.GetChild(i).gameObject.SetActive(true);
        }
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
                // Calculate the new y offset using Mathf.Sin
                float offsetX = Mathf.Sin(Time.time * speed) * amplitude;

                // Update the anchored position while preserving the x-coordinate
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

    public bool IsInTheMine(float y) {
        if (y < -7f) {
            return true;
        }

        return false;
    }

    public void LoadData(GameData data) {
        this.finishedTutorial = data.finishedTutorial;
        this.tutorialScreenIndex = data.tutorialScreenIndex;

        try {
            if (this.finishedTutorial) {
                sessionDelegator.UnlockTeam();
                Destroy(TutorialUIParent);
                return;
            }
        } catch {
            return;
        }

        if (tutorialScreenIndex == 0) {
            AnalyticsDelegator.Instance.StartTutorial();
        }
    
        StartCoroutine(DisplayTutorial());
    }

    public void SaveData(ref GameData data) {
        data.finishedTutorial = this.finishedTutorial;
        data.tutorialScreenIndex = this.tutorialScreenIndex;
    }

    // Reveal a single element, typically a secondary element, and only used after HideAll()
    public void RevealElement(GameObject element) {
        element.SetActive(true);
        AnalyticsDelegator.Instance.OpenTutorialUIPanel(element.name);
    }

    // Used when closing a secondary element
    public void HideElement(GameObject element) {
        element.SetActive(false);
    }

}