using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TutorialManager : MonoBehaviour, IDataPersistence
{
    public GameObject[] tutorialScreens;
    public GameObject bottomControls;
    public GameObject rewardedAdButtons;
    public GameObject settingsButton;
    private bool finishedTutorial;
    private int currentScreenIndex = 0; // Tracks the current tutorial screen
    private GameObject loadingScreen;
    private GameObject[] primaryElements;
    private bool goToNext;
    private bool gameLoaded;
    private AnalyticsDelegator analyticsDelegator;

    void Awake() {
        primaryElements = new GameObject[] { settingsButton };
    }

    void Start()
    {
        StartCoroutine(WaitForGameLoad());

        loadingScreen = GameObject.Find("Loading Screen");
        if (!analyticsDelegator) {
            analyticsDelegator = AnalyticsDelegator.Instance;
        }
        analyticsDelegator = AnalyticsDelegator.Instance;
        
        StartCoroutine(DisplayTutorial());
    }

    private IEnumerator WaitForGameLoad() {
        if (!analyticsDelegator) {
            analyticsDelegator = AnalyticsDelegator.Instance;
        }

        // Skip tutorial if already completed
        // When restarting tutorial, this doesn't immediately destroy the game object,
        // because LoadGame() is only called once when the game is first launched and finishedTutorial is initialized to false
        yield return new WaitUntil(() => gameLoaded);
        
        if (finishedTutorial)
        {
            analyticsDelegator.FinishTutorial();
            Destroy(gameObject);
            yield break;
        }

        analyticsDelegator.StartTutorial();
    }

    private IEnumerator DisplayTutorial()
    {
        // Wait for the loading screen to be destroyed
        yield return new WaitUntil(() => loadingScreen == null);

        while (currentScreenIndex < tutorialScreens.Length)
        {
            GameObject newScreen = Instantiate(tutorialScreens[currentScreenIndex]);
            // false makes it so it keeps its local positioning
            // set parent to safe area
            newScreen.transform.SetParent(transform.GetChild(0), false);
            newScreen.transform.localScale = Vector3.one;

            primaryElements = new GameObject[] { settingsButton, newScreen };

            // Highlight the important stuff
            if (currentScreenIndex == 1 || currentScreenIndex == 2) {
                bottomControls.transform.GetChild(0).gameObject.SetActive(false);
                bottomControls.transform.GetChild(1).gameObject.SetActive(true);
            } else if (currentScreenIndex == 3) {
                bottomControls.transform.GetChild(4).gameObject.SetActive(false);
                bottomControls.transform.GetChild(5).gameObject.SetActive(true);
            } else if (currentScreenIndex == 4) {
                rewardedAdButtons.SetActive(true);
            }

            // Wait for the user to tap/click the screen, but not if it's on a UI element
            yield return new WaitUntil(() => goToNext);
            goToNext = false;

            // Unhighlight the stuff
            if (currentScreenIndex == 1 || currentScreenIndex == 2) {
                bottomControls.transform.GetChild(0).gameObject.SetActive(true);
                bottomControls.transform.GetChild(1).gameObject.SetActive(false);
            } else if (currentScreenIndex == 3) {
                bottomControls.transform.GetChild(4).gameObject.SetActive(true);
                bottomControls.transform.GetChild(5).gameObject.SetActive(false);
            } else if (currentScreenIndex == 4) {
                rewardedAdButtons.SetActive(false);
            }

            Destroy(newScreen);
            newScreen = null;
            currentScreenIndex++;
        }

        // Sync values
        GameObject.Find("Settings Delegator").GetComponent<SettingsDelegator>().UpdateBools();
        finishedTutorial = true;
        GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().SaveGame();
        analyticsDelegator.StartTutorial();
        Destroy(gameObject);
    }

    public void LoadData(GameData data) {
        this.finishedTutorial = data.finishedTutorial;
        GameLoaded();
    }

    public void GameLoaded() {
        gameLoaded = true;
    }

    public void SaveData(ref GameData data) {
        data.finishedTutorial = this.finishedTutorial;
    }

    public void HideAll() {
        for (int i = 0; i < primaryElements.Length; i++) {
            primaryElements[i].SetActive(false);
        }
    }

    // Used after closing a secondary element
    public void RevealAll() {
        for (int i = 0; i < primaryElements.Length; i++) {
            // Reset all buttons back to scale 1. 
            // Need to do this because the button that was pressed down will be at 0.95 still 
            // since it didn't get the pointer up event if it was clicked
            UIButton uiButton = primaryElements[i].GetComponent<UIButton>();
            if (uiButton) {
                StartCoroutine(uiButton.ResetScale());
            }

            primaryElements[i].SetActive(true);
        }
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

        // If a single primary element is inactive, then a menu is open, so don't do go to next
        for (int i = 0; i != primaryElements.Length; i++) {
            if (!primaryElements[i].activeSelf) {
                goToNext = false;
                return;
            }
        }

        goToNext = true;
    }

}