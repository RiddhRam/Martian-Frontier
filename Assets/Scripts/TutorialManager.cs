using System;
using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour, IDataPersistence
{
    public AnalyticsDelegator analyticsDelegator;
    public PlayerState playerState;
    public PlayerVehicleDelegation playerVehicleDelegation;
    public GarageDelegator garageDelegator;
    public RefineryController refineryController;
    public UncollectedMaterialsDelegator uncollectedMaterialsDelegator;
    public SupplyCrateDelegator supplyCrateDelegator;
    public SessionDelegator sessionDelegator;
    public GameObject TutorialUIParent;
    public GameObject[] tutorialScreens;
    public GameObject bottomControls;
    public bool openedGarage = false;
    public bool finishedTutorial;
    public int tutorialScreenIndex = 0; // Tracks the current tutorial screen
    public GameObject loadingScreen;
    public bool readyToGoNext = false;
    public GameObject oreRefineryCanvas;
    private bool goToNext;
    public GameObject newScreen;
    public GameObject leaderboardNoticeIcon;
    public GameObject premiumShopNoticeIcon;

    private IEnumerator DisplayTutorial()
    {
        // Wait for the loading screen to be destroyed
        yield return new WaitUntil(() => !loadingScreen.activeSelf);

        while (tutorialScreenIndex <= tutorialScreens.Length)
        {
            TutorialUIParent.SetActive(true);
            if (tutorialScreenIndex < 3) {
                readyToGoNext = false;
                goToNext = false;

                newScreen = Instantiate(tutorialScreens[tutorialScreenIndex]);
                // false makes it so it keeps its local positioning
                // set parent to safe area
                newScreen.transform.SetParent(TutorialUIParent.transform.GetChild(0), false);
                newScreen.transform.localScale = Vector3.one;

                if (tutorialScreenIndex == 1) {
                    bottomControls.transform.GetChild(0).gameObject.SetActive(false);
                    bottomControls.transform.GetChild(1).gameObject.SetActive(true);
                } else if (tutorialScreenIndex == 2) {
                    oreRefineryCanvas.SetActive(true);
                }

                if (tutorialScreenIndex == 1) {
                    yield return new WaitUntil(() => openedGarage);
                } else {
                    yield return new WaitUntil(() => goToNext);
                }

                if (tutorialScreenIndex == 1) {
                    bottomControls.transform.GetChild(0).gameObject.SetActive(true);
                    bottomControls.transform.GetChild(1).gameObject.SetActive(false);
                } else if (tutorialScreenIndex == 2) {
                    oreRefineryCanvas.SetActive(false);
                }
            
                Destroy(newScreen);
                newScreen = null;
            }

            TutorialUIParent.SetActive(false);
            if (tutorialScreenIndex == 0) {
                yield return new WaitUntil(() => uncollectedMaterialsDelegator.GetMineValue() >= 500);
            } else if (tutorialScreenIndex == 1) {
                yield return new WaitUntil(() => playerVehicleDelegation.vehicleType == "Hauler");
            } else if (tutorialScreenIndex >= 2) {
                yield return new WaitUntil(() => playerState.materialsSold > 0);
            }
            
            tutorialScreenIndex++;
        }

        // Sync values
        GameObject.Find("Settings Delegator").GetComponent<SettingsDelegator>().UpdateBools();
        finishedTutorial = true;

        try {
            playerState.RewardPlayerWithGems(1000, "YOU FINISHED THE TUTORIAL!");
            analyticsDelegator.FinishTutorial();
            // Switch back to first driller and reset mine
            playerVehicleDelegation.SwitchVehicle(garageDelegator.drillers[0]);
            supplyCrateDelegator.ChangeCrateCount(1);
            refineryController.CallResetMineFromButton();
        } catch (Exception ex) {  
            Debug.Log(ex.Message);
        }

        leaderboardNoticeIcon.SetActive(true);
        //premiumShopNoticeIcon.SetActive(true);
        sessionDelegator.UnlockTeam();
        Destroy(TutorialUIParent);
    }

    public void ClickedGarageButton() {
        openedGarage = true;
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

    public void TapToContinue() {

        if (!newScreen.GetComponent<TutorialTextBox>().readyToGoNext) {
            return;
        }

        goToNext = true;
    }

}