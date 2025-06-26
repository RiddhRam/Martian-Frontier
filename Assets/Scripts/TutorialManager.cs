using System;
using System.Collections;
using UnityEngine;
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
    public GameObject playerMessage;
    public bool finishedTutorial;
    public int tutorialScreenIndex = 0; // Tracks the current tutorial screen
    public GameObject loadingScreen;

    [Header("Notice Icons")]
    public GameObject leaderboardNoticeIcon;
    public GameObject premiumShopNoticeIcon;

    private Coroutine arrowAnimation;

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
                RectTransform arrowRT = pointToGarageArrow.GetComponent<RectTransform>();

                Vector2 p = arrowRT.anchoredPosition;
                p.y = 563f;
                arrowRT.anchoredPosition = p;

                arrowRT.offsetMin = new Vector2(185f, arrowRT.offsetMin.y);
                arrowRT.offsetMax = new Vector2(-1328f, arrowRT.offsetMax.y);

                arrowRT.rotation = Quaternion.Euler(0, 0, 180);

                arrowAnimation = StartCoroutine(AnimateArrow(pointToGarageArrow, 90, 1));

                // Wait for panel to open
                yield return new WaitUntil(() => droneUpgradeBayPanel.activeSelf);

                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
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
                StartCoroutine(FlashMessage(playerMessage, 3, 0.3f));

                // Wait for player to accumulate enough cash for first upgrade
                yield return new WaitUntil(() => playerState.GetUserCash() >= 5_000);

                playerMessage.SetActive(false);
            }
            // Point to garage
            else if (tutorialScreenIndex == 5)
            {
                RectTransform arrowRT = pointToGarageArrow.GetComponent<RectTransform>();

                Vector2 p = arrowRT.anchoredPosition;

                p.y = 563f;
                arrowRT.anchoredPosition = p;

                arrowRT.offsetMin = new Vector2(185f, arrowRT.offsetMin.y);
                arrowRT.offsetMax = new Vector2(-1328f, arrowRT.offsetMax.y);

                arrowRT.rotation = Quaternion.Euler(0, 0, 180);

                arrowAnimation = StartCoroutine(AnimateArrow(pointToGarageArrow, 90, 1));

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
            }

            // Tutorial done

            tutorialScreenIndex++;
        }

        // Make sure everything is back to normal
        refineryOresButton.interactable = true;
        proceedTutorialInstruction.SetActive(false);
        refineryUpgradeBayPanel.SetActive(false);
        UIDelegation.Instance.RevealAll();

        // Sync values
        GameObject.Find("Settings Delegator").GetComponent<SettingsDelegator>().UpdateBools();
        finishedTutorial = true;

        try {
            playerState.RewardPlayerWithGems(10000, "YOU FINISHED THE TUTORIAL!");
            supplyCrateDelegator.ChangeCrateCount(1);

            AnalyticsDelegator.Instance.FinishTutorial();
        } catch (Exception ex) {  
            Debug.Log(ex.Message);
        }

        leaderboardNoticeIcon.SetActive(true);
        
        sessionDelegator.UnlockTeam();
        Destroy(TutorialUIParent);
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