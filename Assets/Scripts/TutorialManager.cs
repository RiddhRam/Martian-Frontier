using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour, IDataPersistence
{
    public GameObject[] tutorialScreens;
    public GameObject bottomControls;
    public GameObject rewardedAdButtons;
    private bool finishedTutorial = false;
    private int currentScreenIndex = 0; // Tracks the current tutorial screen
    private GameObject loadingScreen;

    void Start()
    {
        if (finishedTutorial)
        {
            Destroy(gameObject); // Skip tutorial if already completed
            return;
        }

        loadingScreen = GameObject.Find("Loading Screen");
        
        StartCoroutine(DisplayTutorial());
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

            // Wait for the user to tap/click the screen
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0));

            // Wait until the user releases the click before continuing to avoid double skipping
            yield return new WaitUntil(() => Input.GetMouseButtonUp(0));

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
            currentScreenIndex++;
        }

        finishedTutorial = true;
        GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().SaveGame();
        Destroy(gameObject);
    }

    public void LoadData(GameData data) {
        this.finishedTutorial = data.finishedTutorial;
    }

    public void SaveData(ref GameData data) {
        data.finishedTutorial = this.finishedTutorial;
    }
}
