using System;
using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour, IDataPersistence
{
    public AnalyticsDelegator analyticsDelegator;
    public PlayerState playerState;
    public PlayerMovement playerMovement;
    public PlayerVehicleDelegation playerVehicleDelegation;
    public GarageDelegator garageDelegator;
    public RefineryController refineryController;
    //public UncollectedMaterialsDelegator uncollectedMaterialsDelegator;
    public SupplyCrateDelegator supplyCrateDelegator;
    public SessionDelegator sessionDelegator;
    public UpgradesDelegator upgradesDelegator;

    public GameObject ResetMine;
    public GameObject TutorialUIParent;
    public GameObject powerIndicator;
    public GameObject garageArrow;
    public GameObject bottomControls;
    public GameObject enterMineArrow;
    public RectTransform movementJoystick;

    public bool finishedTutorial;
    public int tutorialScreenIndex = 0; // Tracks the current tutorial screen
    public GameObject loadingScreen;
    public bool readyToGoNext = false;
    public GameObject oreRefineryCanvas;
    private bool goToNext;
    public GameObject newScreen;
    public GameObject leaderboardNoticeIcon;
    public GameObject premiumShopNoticeIcon;

    private Coroutine arrowAnimation;

    private IEnumerator DisplayTutorial()
    {
        ResetMine.SetActive(false);

        // Wait for the loading screen to be deactivated
        yield return new WaitUntil(() => !loadingScreen.activeSelf);

        while (tutorialScreenIndex <= 10)
        {
            analyticsDelegator.TutorialStep(tutorialScreenIndex);

            // Load mine
            if (tutorialScreenIndex == 0) {
                playerVehicleDelegation.blockSwitching = true;
                TellPlayerToMove();

                yield return new WaitUntil(() => refineryController.mineRenderer.mineInitialization != 0);

            } 
            // Go into mine
            else if (tutorialScreenIndex == 1) {
                playerVehicleDelegation.blockSwitching = true;

                enterMineArrow.SetActive(true);
                arrowAnimation = StartCoroutine(AnimateArrow(enterMineArrow, 2));

                yield return new WaitUntil(() => IsInTheMine(playerMovement.transform.position.y));
                
                enterMineArrow.SetActive(false);
                if (arrowAnimation != null) {
                    StopCoroutine(arrowAnimation);
                }

            } 
            // Use survey radar
            else if (tutorialScreenIndex == 2) {
                playerVehicleDelegation.blockSwitching = true;
                playerMovement.stopMoving = true;

                // In case player already activated power
                upgradesDelegator.cooldownTimer = 0;
                upgradesDelegator.scannedForOres = false;

                powerIndicator.SetActive(true);
                TutorialUIParent.SetActive(true);
                arrowAnimation = StartCoroutine(AnimateArrow(powerIndicator.transform.GetChild(0).gameObject, 30));
                
                yield return new WaitUntil(() => upgradesDelegator.scannedForOres);

                TutorialUIParent.SetActive(false);
                powerIndicator.SetActive(false);
                if (arrowAnimation != null) {
                    StopCoroutine(arrowAnimation);
                }
                
            } 
            // Mine revealed ores
            else if (tutorialScreenIndex == 3) {
                playerVehicleDelegation.blockSwitching = true;
                TellPlayerToMove();
                yield return new WaitUntil(() => playerState.materialsSold > 10);
            } 

            tutorialScreenIndex++;
        }

        ResetMine.SetActive(true);

        //destroyMaterial.preventDestruction = false;
        playerVehicleDelegation.blockSwitching = false;
        garageDelegator.blockPanelSwitching = false;

        // Sync values
        GameObject.Find("Settings Delegator").GetComponent<SettingsDelegator>().UpdateBools();
        finishedTutorial = true;

        try {
            playerState.RewardPlayerWithGems(5000, "YOU FINISHED THE TUTORIAL!");
            supplyCrateDelegator.ChangeCrateCount(1);
            
            // Switch back to first driller and reset mine
            playerVehicleDelegation.SwitchVehicle(garageDelegator.drillers[0]);
            refineryController.CallResetMineFromButton();

            analyticsDelegator.FinishTutorial();
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

    private IEnumerator AnimateArrow(GameObject arrow, float amplitude) {
        RectTransform rectTransform = arrow.GetComponent<RectTransform>();
        // Save the original position for reference
        Vector2 originalPos = rectTransform.anchoredPosition;

        float speed = 3f;      // Controls the speed of the oscillation

        while (true) {
            // Calculate the new y offset using Mathf.Sin
            float offsetY = Mathf.Sin(Time.time * speed) * amplitude;
            
            // Update the anchored position while preserving the x-coordinate
            rectTransform.anchoredPosition = new Vector2(originalPos.x, originalPos.y + offsetY);
            
            // Wait until the next frame
            yield return null;
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
            analyticsDelegator.StartTutorial();
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
        analyticsDelegator.OpenTutorialUIPanel(element.name);
    }

    // Used when closing a secondary element
    public void HideElement(GameObject element) {
        element.SetActive(false);
    }

}